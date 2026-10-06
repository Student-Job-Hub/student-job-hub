# Secure Password Reset Flow

Zero-dependency Node.js (18+) implementation: API, mock mailer, UI pages, and tests.

## Run
```
npm start        # http://localhost:3000
npm test
```
Demo user: `demo@example.com` / `OldPassword1`. Reset emails are printed to the server console (mock mailer).
Try it: open `/forgot-password`, submit the demo email, copy the link from the console.

## API
| Method | Path | Body | Result |
|---|---|---|---|
| POST | `/api/auth/forgot-password` | `{email}` | Always 200 with a generic message for any well-formed email |
| GET | `/api/auth/reset-password/validate?token=` | | 200 valid / 400 invalid or expired |
| POST | `/api/auth/reset-password` | `{token, newPassword}` | 200 ok / 400 bad token / 422 weak password |
| POST | `/api/auth/login` | `{email, password}` | Session token (for verifying the flow) |

## Security notes
- Tokens: 32 random bytes; only the SHA-256 hash is stored; 30 min expiry; single use; a new request invalidates older tokens.
- No account enumeration: identical response for known/unknown emails; per-email rate limit is silent.
- Passwords hashed with scrypt + salt; policy: 8–128 chars, letter + number.
- Rate limits per IP and per email. Sessions are revoked after a reset.
- `Referrer-Policy: no-referrer`, CSP, `no-store`; the page strips the token from the URL after loading.

## Going to production
- Replace `src/store.js` with a real database (store `tokenHash`, `userId`, `expiresAt`, `usedAt`).
- Replace `src/mailer.js` `send` with SMTP/SES/SendGrid; set `BASE_URL` to your public HTTPS origin.
- Use a shared rate limiter (e.g. Redis), serve over HTTPS, and send the session in an HttpOnly cookie.
- Behind a proxy, derive the client IP from a trusted `X-Forwarded-For`.

## Layout
```
src/    app.js (routes), store.js, mailer.js, passwords.js, rateLimit.js, server.js
public/ index, forgot-password, reset-password pages + JS/CSS
tests/  auth.test.js (node:test)
```
