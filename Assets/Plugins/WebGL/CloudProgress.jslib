mergeInto(LibraryManager.library, {
  HealthCloud_Save: function (jsonPointer) {
    if (window.healthCloud) return window.healthCloud.save(UTF8ToString(jsonPointer));
    return 0;
  },
  HealthCloud_Restored: function () {
    if (window.healthCloud) window.healthCloud.restored();
  },
  HealthCloud_RestoreFailed: function () {
    if (window.healthCloud) window.healthCloud.restoreFailed();
  }
});
