const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { createStore } = require('./store');
const { createMailer } = require('./mailer');
const { createRateLimiter } = require('./rateLimit');
const { hashPassword, verifyPassword, validatePassword } = require('./passwords');

const PUBLIC = path.join(__dirname, '..', 'public');
const PAGES = { '/': 'index.html', '/forgot-password': 'forgot-password.html', '/reset-password': 'reset-password.html' };
const MIME = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8' };
const GENERIC_MSG = 'If an account exists for that email, a reset link has been sent.';
const TOKEN_TTL_MS = 30 * 60 * 1000;
const DUMMY_HASH = hashPassword('dummy-password-1');

function createApp(opts = {}) {
  const now = opts.now || (() => Date.now());
  const store = opts.store || createStore();
  const mailer = opts.mailer || createMailer({ log: opts.logEmails !== false });
  const baseUrl = opts.baseUrl || 'http://localhost:3000';
  const ipLimit = createRateLimiter({ max: opts.ipMax ?? 20, windowMs: 15 * 60 * 1000, now });
  const emailLimit = createRateLimiter({ max: opts.emailMax ?? 3, windowMs: 15 * 60 * 1000, now });

  const send = (res, status, body, headers = {}) => {
    const isJson = typeof body !== 'string' && !Buffer.isBuffer(body);
    res.writeHead(status, {
      'Content-Type': isJson ? 'application/json' : 'text/plain',
      'Cache-Control': 'no-store',
      'X-Content-Type-Options': 'nosniff',
      'Referrer-Policy': 'no-referrer', // keeps the reset token out of Referer headers
      'X-Frame-Options': 'DENY',
      'Content-Security-Policy': "default-src 'self'; frame-ancestors 'none'",
      ...headers,
    });
    res.end(isJson ? JSON.stringify(body) : body);
  };

  function readJson(req) {
    return new Promise((resolve, reject) => {
      let size = 0; const chunks = [];
      req.on('data', (c) => {
        size += c.length;
        if (size > 10 * 1024) { reject(Object.assign(new Error('payload_too_large'), { status: 413 })); req.destroy(); return; }
        chunks.push(c);
      });
      req.on('end', () => {
        try {
          const body = JSON.parse(Buffer.concat(chunks).toString() || '{}');
          resolve(body && typeof body === 'object' ? body : {});
        } catch { reject(Object.assign(new Error('bad_json'), { status: 400 })); }
      });
      req.on('error', reject);
    });
  }

  const routes = {
    // 1. Request a reset link. Always the same response, whether or not the email exists.
    'POST /api/auth/forgot-password': async (req, res, ip) => {
      if (!ipLimit(ip)) return send(res, 429, { error: 'too_many_requests' });
      const { email } = await readJson(req);
      if (typeof email !== 'string' || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email) || email.length > 254) {
        return send(res, 400, { error: 'invalid_email' });
      }
      const user = store.findUserByEmail(email);
      // Per-email limit is enforced silently so it can't be used to probe accounts.
      if (user && emailLimit(email.toLowerCase())) {
        const token = crypto.randomBytes(32).toString('base64url');
        store.saveResetToken(user.id, token, now() + TOKEN_TTL_MS);
        await mailer.send({
          to: user.email,
          subject: 'Reset your password',
          text: `Use this link to reset your password (valid for 30 minutes):\n${baseUrl}/reset-password?token=${token}\n\nIf you did not request this, ignore this email.`,
        });
      }
      send(res, 200, { message: GENERIC_MSG });
    },

    // 2. Check a token before showing the form.
    'GET /api/auth/reset-password/validate': async (req, res, ip, url) => {
      const valid = !!store.findValidResetToken(url.searchParams.get('token'), now());
      send(res, valid ? 200 : 400, valid ? { valid: true } : { valid: false, error: 'invalid_or_expired_token' });
    },

    // 3. Set the new password.
    'POST /api/auth/reset-password': async (req, res, ip) => {
      if (!ipLimit(ip)) return send(res, 429, { error: 'too_many_requests' });
      const { token, newPassword } = await readJson(req);
      const rec = store.findValidResetToken(token, now());
      if (!rec) return send(res, 400, { error: 'invalid_or_expired_token' });
      const user = store.findUserById(rec.userId);
      const problem = validatePassword(newPassword, user.email);
      if (problem) return send(res, 422, { error: 'weak_password', message: problem }); // token stays usable
      store.setPassword(user.id, hashPassword(newPassword));
      store.consumeResetToken(token, now());
      store.deleteSessionsForUser(user.id); // sign out everywhere
      send(res, 200, { message: 'Password updated. You can now log in.' });
    },

    // Minimal login so the flow can be verified end to end.
    'POST /api/auth/login': async (req, res, ip) => {
      if (!ipLimit(ip)) return send(res, 429, { error: 'too_many_requests' });
      const { email, password } = await readJson(req);
      const valid = typeof email === 'string' && typeof password === 'string';
      const user = valid ? store.findUserByEmail(email) : null;
      // Verify against a dummy hash when the user is missing to keep timing similar.
      const matches = verifyPassword(valid ? password : '', user ? user.passwordHash : DUMMY_HASH);
      if (!user || !matches) return send(res, 401, { error: 'invalid_credentials' });
      send(res, 200, { sessionToken: store.createSession(user.id) });
    },

    'GET /api/auth/me': async (req, res) => {
      const token = (req.headers.authorization || '').replace(/^Bearer /, '');
      const user = store.findUserById(store.getSessionUser(token));
      if (!user) return send(res, 401, { error: 'unauthenticated' });
      send(res, 200, { email: user.email });
    },
  };

  const server = http.createServer(async (req, res) => {
    try {
      const url = new URL(req.url, 'http://localhost');
      const ip = req.socket.remoteAddress || 'unknown';
      const handler = routes[`${req.method} ${url.pathname}`];
      if (handler) return await handler(req, res, ip, url);

      if (req.method === 'GET') {
        const file = PAGES[url.pathname] || (/^\/[\w-]+\.(js|css)$/.test(url.pathname) ? url.pathname.slice(1) : null);
        if (file && fs.existsSync(path.join(PUBLIC, file))) {
          return send(res, 200, fs.readFileSync(path.join(PUBLIC, file)), { 'Content-Type': MIME[path.extname(file)] });
        }
      }
      send(res, 404, { error: 'not_found' });
    } catch (err) {
      if (err.status) return send(res, err.status, { error: err.message });
      console.error(err);
      send(res, 500, { error: 'server_error' });
    }
  });

  return { server, store, mailer, seedUser: (email, password) => store.createUser(email, hashPassword(password)) };
}

module.exports = { createApp };
