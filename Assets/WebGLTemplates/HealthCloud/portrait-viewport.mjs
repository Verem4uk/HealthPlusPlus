export function fitPortraitViewport(width, height) {
  if (!Number.isFinite(width) || !Number.isFinite(height) || width <= 0 || height <= 0)
    return { width: 0, height: 0 };
  return { width: Math.min(width, height * .75), height };
}

export function installPortraitViewport(document, window) {
  const viewport = document.querySelector("#game-viewport");
  const canvas = document.querySelector("#unity-canvas");
  const resize = () => {
    const size = fitPortraitViewport(viewport.clientWidth, viewport.clientHeight);
    canvas.style.width = size.width + "px";
    canvas.style.height = size.height + "px";
  };
  new window.ResizeObserver(resize).observe(viewport);
  resize();
}

if (typeof document !== "undefined") installPortraitViewport(document, window);
