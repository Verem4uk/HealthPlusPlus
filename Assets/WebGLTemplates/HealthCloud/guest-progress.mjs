import { emptyProgress, validateProgress } from "./progress-store.mjs";

export class GuestProgress {
  constructor(storage, key) {
    this.storage = storage;
    this.key = key;
    const saved = storage.getItem(key);
    this.state = saved ? JSON.parse(saved) : {
      progress: emptyProgress(), started: false, promptShown: false, claimedUid: null
    };
    validateProgress(this.state.progress);
    if (typeof this.state.started !== "boolean" || typeof this.state.promptShown !== "boolean" ||
        (this.state.claimedUid !== null && typeof this.state.claimedUid !== "string"))
      throw new Error("invalid-guest-progress");
  }

  persist(state) {
    this.storage.setItem(this.key, JSON.stringify(state));
    this.state = state;
  }

  begin() {
    // A guest session after signing out must not reuse an account's claimed copy.
    if (this.state.claimedUid !== null)
      this.persist({ progress: emptyProgress(), started: true, promptShown: false, claimedUid: null });
    else this.persist({ ...this.state, started: true });
    return this.state.progress;
  }

  complete(progress) {
    const shouldPrompt = !this.state.promptShown;
    this.persist({ progress: validateProgress(progress), started: true, promptShown: true, claimedUid: null });
    return shouldPrompt;
  }

  claim(uid) {
    this.persist({ ...this.state, claimedUid: uid });
  }
}

export function planGuestImport(guest, accountProgress) {
  if (guest.state.claimedUid !== null || !guest.state.progress.completedTrainingDay) return "none";
  const account = validateProgress(accountProgress);
  const same = (left, right) => Object.keys(left).every(key => left[key] === right[key]);
  if (same(account, guest.state.progress)) return "same";
  return same(account, emptyProgress()) ? "import" : "choose";
}
