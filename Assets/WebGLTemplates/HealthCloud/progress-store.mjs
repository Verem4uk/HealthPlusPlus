export const emptyProgress = () => ({
  schemaVersion: 1, level: 0, baseNumber: 0, completedTrainingDay: ""
});

export function validateProgress(value) {
  if (!value || value.schemaVersion !== 1 || !Number.isInteger(value.level) || value.level < 0 ||
      !Number.isInteger(value.baseNumber) || value.baseNumber < 0 || value.baseNumber > 1000000 ||
      typeof value.completedTrainingDay !== "string") throw new Error("invalid-progress");
  const day = value.completedTrainingDay;
  if (day && (!/^\d{4}-\d{2}-\d{2}$/.test(day) ||
      !Number.isFinite(Date.parse(day)) || new Date(day).toISOString().slice(0, 10) !== day))
    throw new Error("invalid-progress");
  return { schemaVersion: 1, level: value.level, baseNumber: value.baseNumber, completedTrainingDay: day };
}

// Persist the outbox before starting a network request. One journal belongs to one UID.
export class ProgressStore {
  constructor(storage, key, makeId = () => crypto.randomUUID()) {
    this.storage = storage;
    this.key = key;
    this.makeId = makeId;
    const saved = storage.getItem(key);
    this.state = saved ? JSON.parse(saved) : { revision: 0, pending: [] };
    if (!Number.isSafeInteger(this.state.revision) || this.state.revision < 0 ||
        !Array.isArray(this.state.pending)) throw new Error("invalid-journal");
    for (const item of this.state.pending) {
      if (typeof item.id !== "string") throw new Error("invalid-journal");
      validateProgress(item.progress);
    }
    this.running = null;
  }

  persist(state) {
    this.storage.setItem(this.key, JSON.stringify(state));
    this.state = state;
  }

  reconcile(remote) {
    if (remote) {
      validateProgress(remote.progress);
      if (!Number.isSafeInteger(remote.revision) || remote.revision < 1 ||
          typeof remote.mutationId !== "string") throw new Error("invalid-cloud-document");
    }
    const revision = remote?.revision ?? 0;
    let pending = this.state.pending;
    // A lost response after a successful commit must not look like a conflict.
    if (pending.length && remote?.mutationId === pending[0].id) {
      pending = pending.slice(1);
      this.persist({ revision, pending });
    }
    if (pending.length && this.state.revision !== revision) throw new Error("progress-conflict");
    this.persist({ revision, pending });
    return pending.length ? pending[pending.length - 1].progress : remote?.progress ?? emptyProgress();
  }

  enqueue(progress) {
    const item = { id: this.makeId(), progress: validateProgress(progress) };
    this.persist({ revision: this.state.revision, pending: [...this.state.pending, item] });
  }

  // Called only after the user explicitly chooses a side in the conflict screen.
  resolve(remote, useLocal) {
    if (remote) {
      validateProgress(remote.progress);
      if (!Number.isSafeInteger(remote.revision) || remote.revision < 1 ||
          typeof remote.mutationId !== "string") throw new Error("invalid-cloud-document");
    }
    const local = this.state.pending.at(-1)?.progress;
    this.persist({ revision: remote?.revision ?? 0,
      pending: useLocal && local ? [{ id: this.makeId(), progress: validateProgress(local) }] : [] });
    return this.reconcile(remote);
  }

  flush(commit) {
    if (this.running) return this.running;
    this.running = (async () => {
      while (this.state.pending.length) {
        const item = this.state.pending[0];
        const revision = await commit(this.state.revision, item);
        this.persist({ revision, pending: this.state.pending.slice(1) });
      }
    })().finally(() => { this.running = null; });
    return this.running;
  }
}
