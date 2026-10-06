const $ = (id) => document.getElementById(id);
const token = new URLSearchParams(location.search).get('token') || '';
history.replaceState(null, '', location.pathname); // drop the token from the address bar
function show(text, ok) { $('msg').textContent = text; $('msg').className = 'msg ' + (ok ? 'ok' : 'err'); }
function link(href, text) { const a = document.createElement('a'); a.href = href; a.textContent = text; $('links').replaceChildren(a); }

function invalid() {
  $('f').classList.add('hidden');
  $('sub').textContent = '';
  show('This reset link is invalid or has expired.', false);
  link('/forgot-password', 'Request a new reset link');
}

(async () => {
  try {
    const r = await fetch('/api/auth/reset-password/validate?token=' + encodeURIComponent(token));
    if (!r.ok) return invalid();
    $('sub').textContent = 'Choose a new password (8+ characters, with a letter and a number).';
    $('f').classList.remove('hidden');
  } catch { $('sub').textContent = ''; show('Network error. Please reload.', false); }
})();

$('f').addEventListener('submit', async (e) => {
  e.preventDefault();
  if ($('pw').value !== $('pw2').value) return show('Passwords do not match.', false);
  $('btn').disabled = true;
  try {
    const r = await fetch('/api/auth/reset-password', { method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token, newPassword: $('pw').value }) });
    const d = await r.json();
    if (r.ok) { $('f').classList.add('hidden'); $('sub').textContent = ''; show(d.message, true); link('/', 'Go to log in'); }
    else if (d.error === 'invalid_or_expired_token') invalid();
    else show(d.message || 'Something went wrong.', false);
  } catch { show('Network error. Please try again.', false); }
  $('btn').disabled = false;
});
