mergeInto(LibraryManager.library, {
  HealthAnalytics_Track: function (namePointer, jsonPointer) {
    try {
      if (window.healthAnalytics) {
        window.healthAnalytics.track(UTF8ToString(namePointer), JSON.parse(UTF8ToString(jsonPointer)));
      }
    } catch (error) {
      // Analytics is optional, including when a browser blocks its SDK.
    }
  }
});
