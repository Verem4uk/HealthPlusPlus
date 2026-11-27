export function installFullscreen(document) {
  const target = document.documentElement;
  const canvas = document.querySelector("#unity-canvas");
  const button = document.querySelector("#fullscreen-toggle");
  const request = target.requestFullscreen || target.webkitRequestFullscreen;
  const supported = Boolean(request) && document.fullscreenEnabled !== false && document.webkitFullscreenEnabled !== false;
  let armed = false, pending = false;
  const active = () => Boolean(document.fullscreenElement || document.webkitFullscreenElement);

  function sync() {
    button.textContent = active() ? "Exit full screen" : "Full screen";
    button.setAttribute("aria-label", button.textContent);
    button.setAttribute("aria-pressed", String(active()));
    button.disabled = pending;
  }

  function invoke(action) {
    if (pending) return Promise.resolve(false);
    pending = true;
    sync();
    // Call synchronously inside the tap/click handler to retain user activation.
    try {
      return Promise.resolve(action()).then(() => {
        button.title = "";
        return true;
      }).catch(() => {
        button.title = "Full screen is unavailable right now. You can try again or continue in this window.";
        return false;
      }).finally(() => { pending = false; sync(); });
    } catch {
      pending = false;
      button.title = "Full screen is unavailable right now. You can continue in this window.";
      sync();
      return Promise.resolve(false);
    }
  }

  function enter() {
    if (!supported || active()) return Promise.resolve(false);
    return invoke(() => target.requestFullscreen
      ? target.requestFullscreen({ navigationUI: "hide" }) : target.webkitRequestFullscreen());
  }

  button.onclick = () => {
    if (!active()) return enter();
    const exit = document.exitFullscreen || document.webkitExitFullscreen;
    return exit ? invoke(() => exit.call(document)) : Promise.resolve(false);
  };
  document.addEventListener("fullscreenchange", sync);
  document.addEventListener("webkitfullscreenchange", sync);
  sync();

  return {
    arm() {
      if (armed || !supported) return;
      armed = true;
      button.hidden = false;
      canvas.addEventListener("pointerdown", enter, { once: true });
    }
  };
}

if (typeof document !== "undefined") window.healthFullscreen = installFullscreen(document);
