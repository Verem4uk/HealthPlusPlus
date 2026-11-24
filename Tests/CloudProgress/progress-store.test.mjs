import { test } from 'node:test';
import assert from 'node:assert/strict';
import { ProgressStore, emptyProgress, validateProgress } from '../../Assets/WebGLTemplates/HealthCloud/progress-store.mjs';

function fixture() {
  const values = new Map();
  const storage = { getItem: key => values.get(key) ?? null, setItem: (key, value) => values.set(key, value) };
  let id = 0;
  const open = (uid = 'alice') => new ProgressStore(storage, uid, () => 'operation-' + ++id);
  return { storage, open };
}
const progress = (level = 1) => ({ ...emptyProgress(), level, baseNumber: 12, completedTrainingDay: '2026-11-24' });
const remote = (revision, mutationId, value = progress()) => ({ revision, mutationId, progress: value });

test('restores a new user and validates dates and numbers', () => {
  assert.deepEqual(fixture().open().reconcile(null), emptyProgress());
  for (const invalid of [{ ...progress(), level: -1 }, { ...progress(), schemaVersion: 2 },
    { ...progress(), completedTrainingDay: '2026-02-30' }, { ...progress(), baseNumber: 1.5 }])
    assert.throws(() => validateProgress(invalid), /invalid-progress/);
});

test('offline progress survives reload and stays isolated by account', async () => {
  const { open } = fixture();
  const first = open();
  first.reconcile(remote(3, 'old'));
  first.enqueue(progress(2));
  await assert.rejects(first.flush(async () => { throw new Error('offline'); }), /offline/);
  assert.deepEqual(open().reconcile(remote(3, 'old')), progress(2));
  assert.deepEqual(open('bob').reconcile(null), emptyProgress());
});

test('a stale device cannot overwrite another device', async () => {
  const store = fixture().open();
  store.reconcile(remote(3, 'old'));
  store.enqueue(progress(2));
  assert.throws(() => store.reconcile(remote(4, 'other-device')), /progress-conflict/);
  assert.equal(store.state.pending.length, 1);
});

test('lost acknowledgement is recognized after reload without writing twice', () => {
  const { open } = fixture();
  const store = open();
  store.reconcile(remote(3, 'old'));
  store.enqueue(progress(2));
  const id = store.state.pending[0].id;
  const reopened = open();
  assert.deepEqual(reopened.reconcile(remote(4, id, progress(2))), progress(2));
  assert.equal(reopened.state.pending.length, 0);
});

test('serializes writes and retains a new save enqueued while a request is running', async () => {
  const store = fixture().open();
  store.reconcile(null);
  store.enqueue(progress(1));
  let release;
  const wait = new Promise(resolve => { release = resolve; });
  const calls = [];
  const commit = async (revision, item) => {
    calls.push([revision, item.progress.level]);
    if (calls.length === 1) await wait;
    return revision + 1;
  };
  const flush = store.flush(commit);
  store.enqueue(progress(2));
  assert.equal(store.flush(commit), flush);
  release();
  await flush;
  assert.deepEqual(calls, [[0, 1], [1, 2]]);
  assert.deepEqual(store.state, { revision: 2, pending: [] });
});

test('conflict resolution keeps the explicitly selected snapshot', () => {
  for (const useLocal of [true, false]) {
    const store = fixture().open();
    store.reconcile(remote(2, 'old'));
    store.enqueue(progress(2));
    assert.deepEqual(store.resolve(remote(3, 'other', progress(3)), useLocal), progress(useLocal ? 2 : 3));
    assert.equal(store.state.revision, 3);
    assert.equal(store.state.pending.length, useLocal ? 1 : 0);
  }
});

test('storage failure leaves the previous journal intact', () => {
  const { open, storage } = fixture();
  const store = open();
  store.reconcile(null);
  storage.setItem = () => { throw new Error('quota'); };
  assert.throws(() => store.enqueue(progress()), /quota/);
  assert.equal(store.state.pending.length, 0);
});

test('corrupt cloud data cannot discard pending progress during conflict resolution', () => {
  const store = fixture().open();
  store.enqueue(progress());
  assert.throws(() => store.resolve(remote(4, 'other', { level: -1 }), false), /invalid-progress/);
  assert.equal(store.state.pending.length, 1);
});
