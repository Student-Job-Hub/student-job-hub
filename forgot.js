const f = document.getElementById('f'), msg = document.getElementById('msg'), btn = document.getElementById('btn');
function show(text, ok) { msg.textContent = text; msg.className = 'msg ' + (ok ? 'ok' : 'err'); }
f.addEventListener('submit', async (e) => {
  e.preventDefault(); btn.disabled = true;
  try {
    const r = await fetch('/api/auth/forgot-password', { method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: f.email.value.trim() }) });
    const d = await r.json();
    if (r.ok) show(d.message, true);
    else show(r.status === 429 ? 'Too many requests. Please try again later.' : 'Please enter a valid email address.', false);
  } catch { show('Network error. Please try again.', false); }
  btn.disabled = false;
});
