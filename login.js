const f = document.getElementById('f'), msg = document.getElementById('msg');
function show(text, ok) { msg.textContent = text; msg.className = 'msg ' + (ok ? 'ok' : 'err'); }
f.addEventListener('submit', async (e) => {
  e.preventDefault();
  const r = await fetch('/api/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: f.email.value, password: f.password.value }) });
  show(r.ok ? 'Logged in successfully.' : r.status === 429 ? 'Too many attempts. Try later.' : 'Invalid email or password.', r.ok);
});
