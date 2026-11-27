export function installTheme(document, window) {
  const sound = document.querySelector('#sound-toggle');
  const account = document.querySelector('#account-toggle');
  const menu = document.querySelector('#account-menu');
  const signOut = document.querySelector('#sign-out');
  const signIn = document.querySelector('#account-sign-in');
  let muted = false;
  try { muted = window.localStorage.getItem('health-audio-muted') === '1'; } catch { /* Storage can be unavailable before login. */ }
  function sync(unity = window.healthUnity) {
    sound.setAttribute('aria-pressed', String(muted));
    const label = muted ? 'Unmute sound' : 'Mute sound';
    sound.setAttribute('aria-label', label);
    sound.title = label;
    unity?.SendMessage('HealthAudio', 'SetMuted', muted ? '1' : '0');
  }
  window.healthAudio = { sync };
  sync();
  sound.onclick = () => {
    muted = !muted;
    try { window.localStorage.setItem('health-audio-muted', muted ? '1' : '0'); } catch { /* The toggle still works for this session. */ }
    sync();
  };
  function close() { menu.hidden = true; account.setAttribute('aria-expanded', 'false'); }
  account.onclick = () => {
    signIn.hidden = !signOut.hidden;
    const state = window.healthAccountState?.() ?? { canSignIn: true, message: '' };
    signIn.disabled = !state.canSignIn;
    const note = document.querySelector('#account-note');
    note.textContent = signOut.hidden ? state.message : '';
    note.hidden = !note.textContent;
    menu.hidden = !menu.hidden;
    account.setAttribute('aria-expanded', String(!menu.hidden));
  };
  signIn.onclick = () => { close(); window.healthOpenAccount?.(); };
  signOut.addEventListener('click', close);
  document.addEventListener('pointerdown', event => {
    if (!menu.contains(event.target) && !account.contains(event.target)) close();
  });
  document.addEventListener('keydown', event => { if (event.key === 'Escape') close(); });
}
if (typeof document !== 'undefined') installTheme(document, window);
