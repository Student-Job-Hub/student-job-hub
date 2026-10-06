const crypto = require('crypto');

// scrypt with a per-password salt. Format: scrypt$<saltHex>$<hashHex>
function hashPassword(password) {
  const salt = crypto.randomBytes(16);
  const hash = crypto.scryptSync(password, salt, 64);
  return `scrypt$${salt.toString('hex')}$${hash.toString('hex')}`;
}

function verifyPassword(password, stored) {
  const [scheme, saltHex, hashHex] = String(stored).split('$');
  if (scheme !== 'scrypt' || !saltHex || !hashHex) return false;
  const expected = Buffer.from(hashHex, 'hex');
  const actual = crypto.scryptSync(password, Buffer.from(saltHex, 'hex'), expected.length);
  return crypto.timingSafeEqual(actual, expected);
}

// Returns an error message, or null if the password is acceptable.
function validatePassword(password, email = '') {
  if (typeof password !== 'string') return 'Password is required.';
  if (password.length < 8) return 'Password must be at least 8 characters.';
  if (password.length > 128) return 'Password must be at most 128 characters.';
  if (!/[A-Za-z]/.test(password) || !/\d/.test(password)) return 'Password must include a letter and a number.';
  if (email && password.toLowerCase() === email.toLowerCase()) return 'Password must not be your email.';
  return null;
}

module.exports = { hashPassword, verifyPassword, validatePassword };
