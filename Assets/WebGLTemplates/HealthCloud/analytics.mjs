const sdkUrl = "https://www.gstatic.com/firebasejs/13.4.0/firebase-analytics.js";
const eventFields = {
  login: { method: "string" },
  app_ready: {},
  guest_started: {},
  guest_sign_in_prompt: { level: "number" },
  guest_converted: { account_type: "string" },
  training_start: { level: "number", base_number: "number" },
  training_complete: { level: "number", duration_seconds: "number" }
};

// Analytics is optional: never wait for it before authenticating or saving.
export function createAnalytics({ loadSdk = () => import(sdkUrl), debug = false } = {}) {
  let sdk, instance, initialization, state = "pending";
  let accountStatus = "guest";
  const queue = [];

  function send(event) {
    try { sdk.logEvent(instance, event.name, event.params); }
    catch { /* Blockers and SDK errors must not interrupt a workout. */ }
  }

  return {
    setAccountStatus(value) {
      if (value === "guest" || value === "registered") accountStatus = value;
    },
    initialize(app) {
      if (initialization) return initialization;
      initialization = (async () => {
        try {
          sdk = await loadSdk();
          if (!await sdk.isSupported()) { state = "disabled"; queue.length = 0; return; }
          instance = sdk.initializeAnalytics(app, { config: {
            allow_google_signals: false,
            allow_ad_personalization_signals: false,
            ...(debug ? { debug_mode: true } : {})
          } });
          state = "ready";
          for (const event of queue.splice(0)) send(event);
        } catch {
          state = "disabled";
          queue.length = 0;
        }
      })();
      return initialization;
    },

    track(name, values = {}) {
      if (state === "disabled" || !Object.hasOwn(eventFields, name)) return;
      const params = { platform: "webgl", account_status: accountStatus };
      for (const [field, type] of Object.entries(eventFields[name])) {
        const value = values[field];
        if (typeof value !== type || (type === "number" && (!Number.isFinite(value) || value < 0))) return;
        if (field === "method" && value !== "Google") return;
        if (field === "account_type" && value !== "new" && value !== "existing") return;
        params[field] = value;
      }
      if (debug) params.debug_mode = true;
      const event = { name, params };
      if (state === "ready") send(event);
      else if (queue.length < 32) queue.push(event);
    }
  };
}
