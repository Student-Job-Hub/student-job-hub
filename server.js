const { createApp } = require('./app');

const port = process.env.PORT || 3000;
const app = createApp({ baseUrl: process.env.BASE_URL || `http://localhost:${port}` });

// Demo account so you can try the flow immediately.
app.seedUser('demo@example.com', 'OldPassword1');

app.server.listen(port, () => {
  console.log(`Running at http://localhost:${port}`);
  console.log('Demo user: demo@example.com / OldPassword1');
  console.log('Reset emails are printed in this console (mock mailer).');
});
