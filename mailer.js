// Mock mailer: logs the email and keeps it in memory so tests (and you) can read the reset link.
// Swap `send` for nodemailer/SES/SendGrid in production.
function createMailer({ log = true } = {}) {
  const outbox = [];
  return {
    outbox,
    async send({ to, subject, text }) {
      outbox.push({ to, subject, text, at: new Date() });
      if (log) console.log(`\n[MOCK EMAIL] to: ${to}\nsubject: ${subject}\n${text}\n`);
    },
  };
}

module.exports = { createMailer };
