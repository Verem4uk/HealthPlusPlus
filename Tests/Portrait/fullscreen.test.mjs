import { test } from 'node:test';
import assert from 'node:assert/strict';
import { installFullscreen } from '../../Assets/WebGLTemplates/HealthCloud/fullscreen.mjs';

function node() {
  const listeners = new Map();
  return { hidden: true, disabled: false, attributes: {},
    setAttribute(key, value) { this.attributes[key] = value; },
    addEventListener(name, handler, options) {
      if (!listeners.has(name)) listeners.set(name, []);
      listeners.get(name).push({ handler, once: options?.once });
    },
    fire(name) {
      const entries = [...(listeners.get(name) ?? [])];
      listeners.set(name, entries.filter(entry => !entry.once));
      return Promise.all(entries.map(entry => entry.handler()));
    }
  };
}

function fixture({ prefixed = false, unavailable = false, failure = false, syncFailure = false } = {}) {
  const target = node(), canvas = node(), button = node(), document = node();
  let requests = 0;
  document.documentElement = target;
  document.querySelector = selector => selector === '#unity-canvas' ? canvas : button;
  const enter = options => {
    requests++;
    if (syncFailure) throw new Error('denied');
    if (failure) return Promise.reject(new Error('denied'));
    if (!prefixed) assert.equal(options.navigationUI, 'hide');
    document[prefixed ? 'webkitFullscreenElement' : 'fullscreenElement'] = target;
    void document.fire(prefixed ? 'webkitfullscreenchange' : 'fullscreenchange');
    return Promise.resolve();
  };
  if (!unavailable) target[prefixed ? 'webkitRequestFullscreen' : 'requestFullscreen'] = enter;
  document[prefixed ? 'webkitExitFullscreen' : 'exitFullscreen'] = () => {
    document[prefixed ? 'webkitFullscreenElement' : 'fullscreenElement'] = null;
    void document.fire(prefixed ? 'webkitfullscreenchange' : 'fullscreenchange');
    return Promise.resolve();
  };
  return { target, canvas, button, document, controller: installFullscreen(document), requests: () => requests };
}

test('waits for game readiness and requests fullscreen directly on the first canvas tap only', async () => {
  const f = fixture();
  await f.canvas.fire('pointerdown');
  assert.equal(f.requests(), 0);
  assert.equal(f.button.hidden, true);
  f.controller.arm(); f.controller.arm();
  assert.equal(f.button.hidden, false);
  const request = f.canvas.fire('pointerdown');
  assert.equal(f.requests(), 1); // The API call happens before yielding user activation.
  await request;
  await f.canvas.fire('pointerdown');
  assert.equal(f.requests(), 1);
  assert.equal(f.button.textContent, 'Exit full screen');
});

test('exit and manual re-entry work without trapping the next game tap after Escape', async () => {
  const f = fixture(); f.controller.arm();
  await f.canvas.fire('pointerdown');
  f.document.fullscreenElement = null;
  await f.document.fire('fullscreenchange');
  assert.equal(f.button.textContent, 'Full screen');
  await f.canvas.fire('pointerdown');
  assert.equal(f.requests(), 1);
  await f.button.onclick();
  assert.equal(f.requests(), 2);
  assert.equal(f.button.attributes['aria-pressed'], 'true');
  await f.button.onclick();
  assert.equal(f.button.attributes['aria-pressed'], 'false');
});

test('unsupported or denied fullscreen does not block the app', async () => {
  for (const options of [{ unavailable: true }, { failure: true }, { syncFailure: true }]) {
    const f = fixture(options); f.controller.arm();
    await assert.doesNotReject(f.canvas.fire('pointerdown'));
    assert.equal(f.button.disabled, false);
    assert.equal(f.button.hidden, Boolean(options.unavailable));
    assert.equal(f.button.textContent, 'Full screen');
    if (!options.unavailable) await assert.doesNotReject(f.button.onclick());
  }
});

test('prefixed WebKit enter and exit are supported', async () => {
  const f = fixture({ prefixed: true }); f.controller.arm();
  await f.canvas.fire('pointerdown');
  assert.equal(f.button.textContent, 'Exit full screen');
  await f.button.onclick();
  assert.equal(f.button.textContent, 'Full screen');
});
