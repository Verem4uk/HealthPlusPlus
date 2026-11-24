import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import { createAnalytics } from '../../Assets/WebGLTemplates/HealthCloud/analytics.mjs';

function fixture(overrides = {}, options = {}) {
  const events = [], configurations = [];
  const sdk = {
    isSupported: async () => true,
    initializeAnalytics: (app, config) => { configurations.push(config); return app; },
    logEvent: (app, name, params) => events.push({ name, params }),
    ...overrides
  };
  return { events, configurations, tracker: createAnalytics({ loadSdk: async () => sdk, ...options }) };
}

test('events emitted during SDK loading are sent once in order', async () => {
  const { tracker, events, configurations } = fixture();
  tracker.track('login', { method: 'Google' });
  tracker.track('app_ready');
  await Promise.all([tracker.initialize({}), tracker.initialize({})]);
  assert.deepEqual(events.map(event => event.name), ['login', 'app_ready']);
  assert.equal(configurations.length, 1);
  assert.equal(configurations[0].config.allow_google_signals, false);
  assert.equal(configurations[0].config.allow_ad_personalization_signals, false);
});

test('blocked SDK, unsupported browser and throwing SDK do not escape to the app', async () => {
  const cases = [
    fixture({}, { loadSdk: async () => { throw new Error('blocked'); } }),
    fixture({ isSupported: async () => false }),
    fixture({ initializeAnalytics: () => { throw new Error('storage blocked'); } }),
    fixture({ logEvent: () => { throw new Error('network blocked'); } })
  ];
  for (const { tracker } of cases) {
    tracker.track('app_ready');
    await assert.doesNotReject(tracker.initialize({}));
    assert.doesNotThrow(() => tracker.track('training_start', { level: 1, base_number: 10 }));
  }
});

test('only known event parameters are forwarded, without email, UID or arbitrary payloads', async () => {
  const { tracker, events } = fixture();
  await tracker.initialize({});
  tracker.track('training_start', { level: 2, base_number: 12, email: 'private@example.test', uid: 'private' });
  tracker.track('unknown_event', { email: 'private@example.test' });
  tracker.track('training_complete', { level: 2, duration_seconds: NaN });
  tracker.track('training_start', { level: -1, base_number: 12 });
  assert.deepEqual(events, [{ name: 'training_start', params: { platform: 'webgl', account_status: 'guest', level: 2, base_number: 12 } }]);
});

test('debug mode is opt-in for both automatic and custom events', async () => {
  for (const debug of [false, true]) {
    const { tracker, events, configurations } = fixture({}, { debug });
    await tracker.initialize({});
    tracker.track('app_ready');
    assert.equal(configurations[0].config.debug_mode, debug ? true : undefined);
    assert.equal(events[0].params.debug_mode, debug ? true : undefined);
  }
});

test('guest conversion retains guest workout events and marks later activity as registered', async () => {
  const { tracker, events } = fixture();
  tracker.track('guest_started');
  tracker.track('guest_sign_in_prompt', { level: 1 });
  tracker.setAccountStatus('registered');
  tracker.track('guest_converted', { account_type: 'new' });
  tracker.track('training_start', { level: 1, base_number: 10 });
  await tracker.initialize({});
  assert.deepEqual(events.map(event => [event.name, event.params.account_status]), [
    ['guest_started', 'guest'], ['guest_sign_in_prompt', 'guest'],
    ['guest_converted', 'registered'], ['training_start', 'registered']
  ]);
});

test('a stalled SDK has a bounded event queue and does not block calls', async () => {
  let release;
  const sdkReady = new Promise(resolve => { release = resolve; });
  const events = [];
  const tracker = createAnalytics({ loadSdk: () => sdkReady });
  const initialization = tracker.initialize({});
  for (let i = 0; i < 100; i++) tracker.track('app_ready');
  release({ isSupported: async () => true, initializeAnalytics: () => ({}), logEvent: (...args) => events.push(args) });
  await initialization;
  assert.equal(events.length, 32);
});

test('WebGL bridge decodes events and tolerates unavailable or failing analytics', () => {
  const library = {}, events = [];
  const context = {
    LibraryManager: { library }, mergeInto: Object.assign,
    UTF8ToString: value => value, window: {}
  };
  vm.runInNewContext(fs.readFileSync('Assets/Plugins/WebGL/ProductAnalytics.jslib', 'utf8'), context);
  assert.doesNotThrow(() => library.HealthAnalytics_Track('app_ready', '{}'));
  context.window.healthAnalytics = { track: (name, params) => events.push([name, params.level]) };
  library.HealthAnalytics_Track('training_start', '{"level":2}');
  assert.deepEqual(events, [['training_start', 2]]);
  assert.doesNotThrow(() => library.HealthAnalytics_Track('training_start', 'invalid-json'));
  context.window.healthAnalytics.track = () => { throw new Error('blocked'); };
  assert.doesNotThrow(() => library.HealthAnalytics_Track('app_ready', '{}'));
});
