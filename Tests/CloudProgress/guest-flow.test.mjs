import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import { ProgressStore, emptyProgress, validateProgress } from '../../Assets/WebGLTemplates/HealthCloud/progress-store.mjs';
import { GuestProgress, planGuestImport } from '../../Assets/WebGLTemplates/HealthCloud/guest-progress.mjs';

const finished = { ...emptyProgress(), baseNumber: 11, completedTrainingDay: '2026-11-24' };
function memory() {
  const values = new Map();
  return { getItem: key => values.get(key) ?? null, setItem: (key, value) => values.set(key, value) };
}
const settle = async () => { for (let i = 0; i < 8; i++) await new Promise(resolve => setImmediate(resolve)); };

async function browser({ storage = memory(), remote = null, user = null, blockedSdk = false, popupError = false, uploadError = false } = {}) {
  const elements = new Map();
  for (const id of ['save-status', 'account-gate', 'account-message', 'sign-in', 'continue-guest', 'retry', 'conflict', 'sign-out', 'use-local', 'use-cloud'])
    elements.set('#' + id, { hidden: ['sign-in', 'continue-guest', 'retry', 'conflict', 'sign-out'].includes(id), disabled: false, textContent: '' });
  const events = [], restores = [];
  let observer, documentData = remote, uid = user?.uid ?? 'alice';
  const authSdk = {
    getAuth: () => ({}), browserLocalPersistence: {}, setPersistence: async () => {},
    GoogleAuthProvider: class {}, getAdditionalUserInfo: () => ({ isNewUser: !remote }),
    onAuthStateChanged: (auth, callback) => { observer = callback; callback(user); },
    signInWithPopup: async () => {
      if (popupError) throw new Error('popup-closed');
      const signedIn = { uid }; observer(signedIn); return { user: signedIn };
    },
    signOut: async () => observer(null)
  };
  const firestore = {
    getFirestore: () => ({}), doc: (db, collection, id) => ({ id }), serverTimestamp: () => 'server-time',
    getDocFromServer: async () => ({ exists: () => documentData !== null, data: () => documentData }),
    runTransaction: async (db, operation) => {
      if (uploadError) throw new Error('offline');
      return operation({
      get: async () => ({ exists: () => documentData !== null, data: () => documentData }),
      set: (ref, data) => { documentData = structuredClone(data); }
      });
    }
  };
  const context = {
    ProgressStore, emptyProgress, validateProgress, GuestProgress, planGuestImport,
    createAnalytics: () => ({ initialize: async () => {}, setAccountStatus() {}, track: (name, params) => events.push({ name, params }) }),
    firebaseConfig: { projectId: 'test', apiKey: 'public', authDomain: 'test.firebaseapp.com', measurementId: 'G-TEST' },
    document: { querySelector: id => elements.get(id) },
    location: { search: '', reload() { context.reloaded = true; } },
    URLSearchParams, localStorage: storage, console: { error() {} },
    navigator: { locks: { request: (key, options, callback) => callback({}) } },
    setInterval() {},
    window: { addEventListener() {}, startHealthUnity: async () => ({ SendMessage: (object, method, json) => {
      restores.push(JSON.parse(json)); context.window.healthCloud.restored();
    } }) },
    loadFirebase: async url => {
      if (blockedSdk) throw new Error('blocked');
      return url.endsWith('firebase-app.js') ? { initializeApp: () => ({}) } :
        url.endsWith('firebase-auth.js') ? authSdk : firestore;
    }
  };
  const script = fs.readFileSync('Assets/WebGLTemplates/HealthCloud/cloud.js', 'utf8')
    .replace(/^import .*;\r?\n/gm, '').replaceAll('import(base +', 'loadFirebase(base +');
  vm.runInNewContext(script, context);
  await settle();
  return { elements, events, restores, storage, context, cloud: context.window.healthCloud,
    remote: () => documentData, click: async id => { await elements.get('#' + id).onclick(); await settle(); } };
}

test('first visit offers Google and guest without starting Unity or opening Google automatically', async () => {
  const page = await browser();
  assert.equal(page.elements.get('#sign-in').hidden, false);
  assert.equal(page.elements.get('#continue-guest').hidden, false);
  assert.equal(page.restores.length, 0);
  assert.equal(page.events.length, 0);
});

test('guest completion persists progress, locks training and requires sign-in again after reload', async () => {
  const page = await browser();
  await page.click('continue-guest');
  assert.deepEqual(page.restores, [emptyProgress()]);
  assert.equal(page.elements.get('#account-gate').hidden, true);
  assert.equal(page.cloud.save(JSON.stringify(finished)), 0);
  assert.equal(page.elements.get('#account-gate').hidden, false);
  assert.equal(page.elements.get('#sign-in').hidden, false);
  assert.equal(page.elements.get('#continue-guest').hidden, true);
  assert.equal(page.events.filter(event => event.name === 'guest_sign_in_prompt').length, 1);
  const reopened = await browser({ storage: page.storage });
  assert.equal(reopened.elements.get('#continue-guest').hidden, true);
  assert.equal(reopened.restores.length, 0);
});

test('guest upgrade uploads the completed workout and restores it into the existing Unity instance', async () => {
  const page = await browser();
  await page.click('continue-guest');
  page.cloud.save(JSON.stringify(finished));
  await page.click('sign-in');
  assert.deepEqual(page.remote().progress, finished);
  assert.deepEqual(page.restores.at(-1), finished);
  assert.equal(page.elements.get('#account-gate').hidden, true);
  assert.equal(page.events.filter(event => event.name === 'guest_converted').length, 1);
  assert.equal(page.cloud.save(JSON.stringify(finished)), 1);
});

test('existing account progress is not overwritten without an explicit choice', async () => {
  const account = { ...finished, level: 2, baseNumber: 25 };
  const page = await browser({ remote: { progress: account, revision: 3, mutationId: 'old' } });
  await page.click('continue-guest');
  page.cloud.save(JSON.stringify(finished));
  await page.click('sign-in');
  assert.equal(page.elements.get('#conflict').hidden, false);
  assert.deepEqual(page.remote().progress, account);
  await page.click('use-cloud');
  assert.deepEqual(page.restores.at(-1), account);
  assert.deepEqual(page.remote().progress, account);
});

test('choosing the guest copy explicitly replaces existing account progress', async () => {
  const page = await browser({ remote: { progress: { ...finished, level: 2 }, revision: 3, mutationId: 'old' } });
  await page.click('continue-guest');
  page.cloud.save(JSON.stringify(finished));
  await page.click('sign-in');
  await page.click('use-local');
  assert.deepEqual(page.remote().progress, finished);
  assert.equal(page.remote().revision, 4);
  assert.deepEqual(page.restores.at(-1), finished);
});

test('returning signed-in user loads cloud progress without an onboarding choice', async () => {
  const page = await browser({ user: { uid: 'alice' }, remote: { progress: finished, revision: 2, mutationId: 'saved' } });
  assert.deepEqual(page.restores, [finished]);
  assert.equal(page.elements.get('#account-gate').hidden, true);
  assert.equal(page.elements.get('#continue-guest').hidden, true);
});

test('direct Google login is not counted as guest conversion', async () => {
  const page = await browser();
  await page.click('sign-in');
  assert.equal(page.events.filter(event => event.name === 'login').length, 1);
  assert.equal(page.events.filter(event => event.name === 'guest_converted').length, 0);
});

test('cancelled Google login keeps the guest gate and is not counted as a conversion', async () => {
  const page = await browser({ popupError: true });
  await page.click('continue-guest');
  page.cloud.save(JSON.stringify(finished));
  await page.click('sign-in');
  assert.equal(page.elements.get('#account-gate').hidden, false);
  assert.equal(page.elements.get('#continue-guest').hidden, true);
  assert.equal(page.events.filter(event => event.name === 'guest_converted').length, 0);
  assert.equal(page.events.filter(event => event.name === 'login').length, 0);
});

test('failed guest upload preserves the result in the account outbox without unlocking the app', async () => {
  const page = await browser({ uploadError: true });
  await page.click('continue-guest');
  page.cloud.save(JSON.stringify(finished));
  await page.click('sign-in');
  assert.equal(page.remote(), null);
  assert.equal(page.elements.get('#account-gate').hidden, false);
  const journal = new ProgressStore(page.storage, 'health-progress:v1:test:alice');
  assert.deepEqual(journal.state.pending.at(-1).progress, finished);
  assert.equal(page.restores.length, 1);
});

test('blocked Firebase SDK still allows the first guest workout', async () => {
  const page = await browser({ blockedSdk: true });
  assert.equal(page.elements.get('#continue-guest').hidden, false);
  await page.click('continue-guest');
  assert.deepEqual(page.restores, [emptyProgress()]);
});

test('claimed guest data cannot silently migrate to a second account', () => {
  const storage = memory();
  const guest = new GuestProgress(storage, 'guest');
  guest.begin(); guest.complete(finished); guest.claim('alice');
  assert.equal(planGuestImport(guest, emptyProgress()), 'none');
  assert.deepEqual(new GuestProgress(storage, 'guest').begin(), emptyProgress());
});

test('WebGL save bridge returns the permission to continue training', () => {
  const library = {};
  const context = { LibraryManager: { library }, mergeInto: Object.assign, UTF8ToString: value => value, window: {} };
  vm.runInNewContext(fs.readFileSync('Assets/Plugins/WebGL/CloudProgress.jslib', 'utf8'), context);
  assert.equal(library.HealthCloud_Save('{}'), 0);
  context.window.healthCloud = { save: () => 0 };
  assert.equal(library.HealthCloud_Save('{}'), 0);
  context.window.healthCloud.save = () => 1;
  assert.equal(library.HealthCloud_Save('{}'), 1);
});
