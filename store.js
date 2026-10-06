const crypto = require('crypto');

const sha256 = (s) => crypto.createHash('sha256').update(s).digest('hex');

// In-memory store. Replace with a real DB; the method names map to simple queries.
// Only the SHA-256 hash of a reset token is stored, never the token itself.
function createStore() {
  const users = new Map();      // id -> {id, email, passwordHash}
  const byEmail = new Map();    // email -> id
  const resetTokens = new Map();// tokenHash -> {userId, expiresAt, usedAt}
  const sessions = new Map();   // sessionToken -> userId
  let nextId = 1;

  return {
    sha256,
    createUser(email, passwordHash) {
      const user = { id: nextId++, email: email.toLowerCase(), passwordHash };
      users.set(user.id, user);
      byEmail.set(user.email, user.id);
      return user;
    },
    findUserByEmail: (email) => users.get(byEmail.get(String(email).toLowerCase())) || null,
    findUserById: (id) => users.get(id) || null,
    setPassword(userId, passwordHash) { users.get(userId).passwordHash = passwordHash; },

    saveResetToken(userId, rawToken, expiresAt) {
      // Invalidate any earlier tokens for this user.
      for (const [h, t] of resetTokens) if (t.userId === userId) resetTokens.delete(h);
      resetTokens.set(sha256(rawToken), { userId, expiresAt, usedAt: null });
    },
    // Returns the token record if it exists, is unused, and unexpired.
    findValidResetToken(rawToken, now) {
      if (typeof rawToken !== 'string' || rawToken.length < 20 || rawToken.length > 200) return null;
      const rec = resetTokens.get(sha256(rawToken));
      if (!rec || rec.usedAt || rec.expiresAt <= now) return null;
      return rec;
    },
    consumeResetToken(rawToken, now) { resetTokens.get(sha256(rawToken)).usedAt = now; },

    createSession(userId) {
      const token = crypto.randomBytes(32).toString('hex');
      sessions.set(token, userId);
      return token;
    },
    getSessionUser: (token) => sessions.get(token) || null,
    deleteSessionsForUser(userId) {
      for (const [t, id] of sessions) if (id === userId) sessions.delete(t);
    },
  };
}

module.exports = { createStore };
