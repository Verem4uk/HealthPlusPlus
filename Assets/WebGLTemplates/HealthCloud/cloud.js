import { firebaseConfig } from "./firebase-config.js";
import { ProgressStore, validateProgress } from "./progress-store.mjs";
import { GuestProgress, planGuestImport } from "./guest-progress.mjs";
import { createAnalytics } from "./analytics.mjs";

const analytics = createAnalytics({ debug: new URLSearchParams(location.search).get("analytics_debug") === "1" });
window.healthAnalytics = analytics;
const status = document.querySelector("#save-status");
const gate = document.querySelector("#account-gate");
const message = document.querySelector("#account-message");
const login = document.querySelector("#sign-in");
const continueGuest = document.querySelector("#continue-guest");
const retry = document.querySelector("#retry");
const conflict = document.querySelector("#conflict");
let guest, store, unity, auth, db, ref, sdk, activeUid;
let guestMode = false, starting = false, restoring = false, failed = false;
let appReadyTracked = false, importingGuest = false;

function block(text) {
  gate.hidden = false;
  message.textContent = text;
  login.hidden = true;
  continueGuest.hidden = true;
  conflict.hidden = true;
}

function requireSignIn() {
  block("Great job completing your first workout! To save your progress and continue training, sign in with Google.");
  login.hidden = false;
  status.textContent = "Progress saved on this device. Sign in to continue.";
}

function offerStart() {
  if (guest.state.claimedUid === null && guest.state.progress.completedTrainingDay) {
    requireSignIn();
    return;
  }
  block("Sign in to restore your progress, or try your first workout as a guest.");
  login.hidden = false;
  continueGuest.hidden = false;
}

function describe(value) {
  return value ? "level " + (value.level + 1) + ", workload " + value.baseNumber +
    ", last workout: " + (value.completedTrainingDay || "none") : "no saved progress";
}

function showConflict(local, account) {
  block(importingGuest
    ? "This account already has progress. Choose which progress you want to keep."
    : "Your progress changed on another device. Choose which progress you want to keep.");
  conflict.hidden = false;
  retry.hidden = true;
  document.querySelector("#use-local").textContent = "This device: " + describe(local);
  document.querySelector("#use-cloud").textContent = "Account: " + describe(account);
}

function fail(error) {
  console.error("Cloud save:", error.code ?? error.message);
  if (error.message === "progress-conflict") {
    importingGuest = false;
    failed = true;
    showConflict(store.state.pending.at(-1)?.progress, null);
    void load().then(remote => {
      document.querySelector("#use-cloud").textContent = "Account: " + describe(remote?.progress);
    }).catch(() => {});
  } else {
    status.textContent = store?.state.pending.length
      ? "Saved on this device; waiting to sync" : "Could not connect to cloud saves";
    if (!unity || guestMode) block("Could not load or sync your account progress. Check your connection and try again.");
    retry.hidden = false;
  }
}

async function commit(expectedRevision, item) {
  return sdk.runTransaction(db, async transaction => {
    const snap = await transaction.get(ref);
    const remote = snap.exists() ? snap.data() : null;
    if (remote?.mutationId === item.id) return remote.revision;
    if ((remote?.revision ?? 0) !== expectedRevision) throw new Error("progress-conflict");
    const revision = expectedRevision + 1;
    transaction.set(ref, { revision, mutationId: item.id, progress: item.progress, updatedAt: sdk.serverTimestamp() });
    return revision;
  });
}

async function flush() {
  if (!store || failed) return false;
  try {
    status.textContent = "Saving…";
    await store.flush(commit);
    status.textContent = "Progress saved to the cloud";
    retry.hidden = true;
    return true;
  } catch (error) { fail(error); return false; }
}

async function load() {
  const snap = await sdk.getDocFromServer(ref);
  return snap.exists() ? snap.data() : null;
}

async function start(progress) {
  block("Loading Health++…");
  if (!unity) unity = await window.startHealthUnity();
  unity.SendMessage("HealthCloudProgress", "Restore", JSON.stringify(progress));
}

async function restore() {
  if (restoring || failed || !store) return;
  restoring = true;
  retry.hidden = true;
  try {
    block("Loading your progress…");
    const remote = await load();
    let progress = store.reconcile(remote);
    const plan = planGuestImport(guest, progress);
    if (plan === "choose") {
      importingGuest = true;
      showConflict(guest.state.progress, progress);
      return;
    }
    if (plan === "import") {
      progress = guest.state.progress;
      store.enqueue(progress); // Persist in this UID's outbox before claiming the guest copy.
    }
    if (plan === "import" || plan === "same") guest.claim(activeUid);
    if (!remote && !store.state.pending.length) store.enqueue(progress);
    // When upgrading a guest session, keep the gate until its upload is confirmed.
    const synced = await flush();
    if (!failed && (!guestMode || synced)) {
      guestMode = false;
      await start(progress);
    }
  } catch (error) { fail(error); }
  finally { restoring = false; }
}

continueGuest.onclick = async () => {
  if (starting || activeUid) return;
  starting = true;
  try {
    const wasStarted = guest.state.started && guest.state.claimedUid === null;
    const progress = guest.begin();
    if (progress.completedTrainingDay) { requireSignIn(); return; }
    guestMode = true;
    analytics.setAccountStatus("guest");
    if (!wasStarted) analytics.track("guest_started");
    status.textContent = "Guest workout";
    await start(progress);
  } catch (error) {
    block("Could not open your guest progress. Check that browser storage is available and reload this page.");
    console.error(error);
  } finally { starting = false; }
};

window.healthCloud = {
  save(json) {
    try {
      const progress = validateProgress(JSON.parse(json));
      if (guestMode) {
        const firstPrompt = guest.complete(progress);
        requireSignIn();
        if (firstPrompt) analytics.track("guest_sign_in_prompt", { level: progress.level + 1 });
        return 0; // Lock Unity's Play() until registration and account restore.
      }
      store.enqueue(progress);
      void flush();
      return 1;
    } catch (error) {
      failed = true;
      block("Could not save progress on this device. Keep this page open and check browser storage.");
      status.textContent = "Save failed";
      console.error(error);
      return 0;
    }
  },
  restored() {
    gate.hidden = true;
    if (!appReadyTracked) {
      appReadyTracked = true;
      analytics.track("app_ready");
    }
  },
  restoreFailed() {
    failed = true;
    block("This progress is not compatible with this app version. Your saved copy is safe. Please update the app or contact support.");
  }
};

for (const useLocal of [true, false]) {
  document.querySelector(useLocal ? "#use-local" : "#use-cloud").onclick = async () => {
    conflict.hidden = true;
    try {
      const remote = await load();
      if (importingGuest) {
        if (useLocal) {
          store.resolve(remote, false);
          store.enqueue(guest.state.progress);
        } else store.reconcile(remote);
        guest.claim(activeUid);
        importingGuest = false;
      } else {
        store.resolve(remote, useLocal);
        if (unity && !guestMode) { location.reload(); return; }
      }
      failed = false;
      await restore();
    } catch (error) { fail(error); }
  };
}

retry.onclick = () => {
  if (!store) location.reload();
  else if (!unity || guestMode) void restore();
  else void flush();
};
window.addEventListener("online", () => {
  if (!store || importingGuest) return;
  if (!unity || guestMode) void restore(); else void flush();
});
setInterval(() => {
  if (store?.state.pending.length && !importingGuest) {
    if (guestMode) void restore(); else void flush();
  }
}, 15000);
window.addEventListener("beforeunload", event => {
  if (store?.state.pending.length) { event.preventDefault(); event.returnValue = ""; }
});

async function initialize() {
  guest = new GuestProgress(localStorage, "health-guest:v1:" + firebaseConfig?.projectId);
  if (!firebaseConfig?.projectId || !firebaseConfig?.apiKey || !firebaseConfig?.authDomain) {
    offerStart();
    login.disabled = true;
    return;
  }
  const base = "https://www.gstatic.com/firebasejs/13.4.0/";
  const [appSdk, authSdk, firestoreSdk] = await Promise.all([
    import(base + "firebase-app.js"), import(base + "firebase-auth.js"), import(base + "firebase-firestore.js")
  ]);
  sdk = firestoreSdk;
  const app = appSdk.initializeApp(firebaseConfig);
  if (firebaseConfig.measurementId) void analytics.initialize(app);
  auth = authSdk.getAuth(app);
  db = sdk.getFirestore(app);
  await authSdk.setPersistence(auth, authSdk.browserLocalPersistence);
  login.onclick = () => {
    login.disabled = true;
    const convertingGuest = guest.state.started && guest.state.claimedUid === null;
    authSdk.signInWithPopup(auth, new authSdk.GoogleAuthProvider())
      .then(result => {
        analytics.setAccountStatus("registered");
        analytics.track("login", { method: "Google" });
        if (convertingGuest) analytics.track("guest_converted", {
          account_type: authSdk.getAdditionalUserInfo(result)?.isNewUser ? "new" : "existing"
        });
      })
      .catch(error => {
        console.error(error.code);
        block("Could not sign in. Allow the Google sign-in window and try again.");
        login.hidden = false;
        if (!unity && !guest.state.progress.completedTrainingDay) continueGuest.hidden = false;
      }).finally(() => { login.disabled = false; });
  };
  authSdk.onAuthStateChanged(auth, user => {
    if (activeUid && user?.uid !== activeUid) { location.reload(); return; }
    if (!user) {
      if (!guestMode) offerStart();
      return;
    }
    if (activeUid) return;
    // External authentication during an unfinished workout cannot replace its state.
    if (unity && guestMode && !guest.state.progress.completedTrainingDay) { location.reload(); return; }
    activeUid = user.uid;
    analytics.setAccountStatus("registered");
    block("Connecting your account…");
    document.querySelector("#sign-out").hidden = false;
    document.querySelector("#sign-out").onclick = () => authSdk.signOut(auth).catch(fail);
    ref = sdk.doc(db, "healthPlusPlusProgress", user.uid);
    if (!navigator.locks) { block("Cloud saves require a modern browser and HTTPS."); return; }
    void navigator.locks.request("health-progress:" + firebaseConfig.projectId + ":" + user.uid,
      { ifAvailable: true }, async lock => {
        if (!lock) { block("Health++ is already open in another tab. Close that tab and reload this page."); return; }
        try {
          store = new ProgressStore(localStorage, "health-progress:v1:" + firebaseConfig.projectId + ":" + user.uid);
          await restore();
        } catch (error) { fail(error); }
        await new Promise(() => {});
      }).catch(fail);
  }, fail);
}

initialize().catch(error => {
  console.error(error);
  if (guest && !unity && !activeUid) {
    offerStart();
    login.disabled = true;
    retry.hidden = false;
  } else {
    block("Could not open browser storage. Check your browser settings and reload this page.");
    retry.hidden = false;
  }
});
