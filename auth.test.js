const { test, before, after, beforeEach } = require('node:test');
const assert = require('node:assert/strict');
const { createApp } = require('../src/app');

let app, base, clock;
const EMAIL = 'user@example.com', OLD = 'OldPassword1', NEW = 'NewPassword2';

before(async () => {
  clock = { t: Date.now() };
  app = createApp({ now: () => clock.t, logEmails: false, baseUrl: 'http://test', ipMax: 1000, emailMax: 3 });
  app.seedUser(EMAIL, OLD);
  await new Promise((r) => app.server.listen(0, r));
  base = `http://127.0.0.1:${app.server.address().port}`;
});
after(() => app.server.close());
beforeEach(() => { app.mailer.outbox.length = 0; clock.t += 60 * 60 * 1000; }); // fresh rate-limit window each test

const post = (path, body) => fetch(base + path, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
const login = (email, password) => post('/api/auth/login', { email, password });
const lastToken = () => new URL(app.mailer.outbox.at(-1).text.match(/https?:\/\/\S+/)[0]).searchParams.get('token');
async function requestToken(email = EMAIL) { await post('/api/auth/forgot-password', { email }); return lastToken(); }

test('success: request -> validate -> reset -> login with new password only', async () => {
  const token = await requestToken();
  assert.equal(app.mailer.outbox.length, 1);
  assert.equal((await fetch(`${base}/api/auth/reset-password/validate?token=${token}`)).status, 200);

  const r = await post('/api/auth/reset-password', { token, newPassword: NEW });
  assert.equal(r.status, 200);
  assert.equal((await login(EMAIL, NEW)).status, 200);
  assert.equal((await login(EMAIL, OLD)).status, 401);
  // restore for later tests
  const t2 = await requestToken();
  await post('/api/auth/reset-password', { token: t2, newPassword: OLD });
});

test('unknown email gets the same response and no email is sent', async () => {
  const known = await post('/api/auth/forgot-password', { email: EMAIL });
  const unknown = await post('/api/auth/forgot-password', { email: 'nobody@example.com' });
  assert.equal(unknown.status, known.status);
  assert.deepEqual(await unknown.json(), await known.json());
  assert.equal(app.mailer.outbox.length, 1);
});

test('malformed email is rejected', async () => {
  assert.equal((await post('/api/auth/forgot-password', { email: 'nope' })).status, 400);
  assert.equal((await post('/api/auth/forgot-password', {})).status, 400);
});

test('invalid token is rejected by validate and reset', async () => {
  const bad = 'x'.repeat(43);
  assert.equal((await fetch(`${base}/api/auth/reset-password/validate?token=${bad}`)).status, 400);
  assert.equal((await fetch(`${base}/api/auth/reset-password/validate`)).status, 400);
  const r = await post('/api/auth/reset-password', { token: bad, newPassword: NEW });
  assert.equal(r.status, 400);
  assert.equal((await r.json()).error, 'invalid_or_expired_token');
});

test('expired token is rejected', async () => {
  const token = await requestToken();
  clock.t += 31 * 60 * 1000;
  assert.equal((await fetch(`${base}/api/auth/reset-password/validate?token=${token}`)).status, 400);
  assert.equal((await post('/api/auth/reset-password', { token, newPassword: NEW })).status, 400);
  assert.equal((await login(EMAIL, OLD)).status, 200); // password unchanged
});

test('token is single use', async () => {
  const token = await requestToken();
  assert.equal((await post('/api/auth/reset-password', { token, newPassword: NEW })).status, 200);
  assert.equal((await post('/api/auth/reset-password', { token, newPassword: 'Another3Pass' })).status, 400);
  const t2 = await requestToken();
  await post('/api/auth/reset-password', { token: t2, newPassword: OLD });
});

test('a newer request invalidates the older token', async () => {
  const first = await requestToken();
  const second = await requestToken();
  assert.notEqual(first, second);
  assert.equal((await post('/api/auth/reset-password', { token: first, newPassword: NEW })).status, 400);
  assert.equal((await post('/api/auth/reset-password', { token: second, newPassword: NEW })).status, 200);
  const t3 = await requestToken();
  await post('/api/auth/reset-password', { token: t3, newPassword: OLD });
});

test('weak password is rejected and the token stays usable', async () => {
  const token = await requestToken();
  for (const pw of ['short1', 'allletters', '12345678', EMAIL, undefined]) {
    const r = await post('/api/auth/reset-password', { token, newPassword: pw });
    assert.equal(r.status, 422, String(pw));
  }
  assert.equal((await post('/api/auth/reset-password', { token, newPassword: NEW })).status, 200);
  assert.equal((await login(EMAIL, NEW)).status, 200);
  const t2 = await requestToken();
  await post('/api/auth/reset-password', { token: t2, newPassword: OLD });
});

test('reset signs out existing sessions', async () => {
  const { sessionToken } = await (await login(EMAIL, OLD)).json();
  const me = () => fetch(base + '/api/auth/me', { headers: { Authorization: `Bearer ${sessionToken}` } });
  assert.equal((await me()).status, 200);
  const token = await requestToken();
  await post('/api/auth/reset-password', { token, newPassword: NEW });
  assert.equal((await me()).status, 401);
  const t2 = await requestToken();
  await post('/api/auth/reset-password', { token: t2, newPassword: OLD });
});

test('only the token hash is stored, not the token', async () => {
  const token = await requestToken();
  assert.ok(app.store.findValidResetToken(token, clock.t));
  assert.notEqual(app.store.sha256(token), token);
});

test('per-email rate limit silently stops further emails', async () => {
  for (let i = 0; i < 5; i++) {
    assert.equal((await post('/api/auth/forgot-password', { email: EMAIL })).status, 200);
  }
  assert.equal(app.mailer.outbox.length, 3);
});

test('pages are served and unknown routes 404', async () => {
  for (const p of ['/', '/forgot-password', '/reset-password']) {
    const r = await fetch(base + p);
    assert.equal(r.status, 200);
    assert.equal(r.headers.get('referrer-policy'), 'no-referrer');
  }
  assert.equal((await fetch(base + '/../package.json')).status, 404);
});
