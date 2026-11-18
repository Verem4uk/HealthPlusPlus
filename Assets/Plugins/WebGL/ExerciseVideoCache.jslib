mergeInto(LibraryManager.library, {
  $HealthVideoStorage: { nextId: 1, results: {} },

  HealthVideo_CreateBlobUrl: function (pathPointer) {
    try {
      var bytes = FS.readFile(UTF8ToString(pathPointer));
      var url = URL.createObjectURL(new Blob([bytes], { type: 'video/mp4' }));
      var size = lengthBytesUTF8(url) + 1;
      var pointer = _malloc(size);
      stringToUTF8(url, pointer, size);
      return pointer;
    } catch (error) {
      console.warn('Could not open cached exercise video:', error);
      return 0;
    }
  },

  HealthVideo_ReleaseBlobUrl: function (urlPointer) {
    URL.revokeObjectURL(UTF8ToString(urlPointer));
  },

  HealthVideo_BeginCacheSync__deps: ['$HealthVideoStorage'],
  HealthVideo_BeginCacheSync: function () {
    var id = HealthVideoStorage.nextId++;
    HealthVideoStorage.results[id] = 0;
    FS.syncfs(false, function (error) {
      HealthVideoStorage.results[id] = error ? -1 : 1;
    });
    return id;
  },

  HealthVideo_GetCacheSyncState__deps: ['$HealthVideoStorage'],
  HealthVideo_GetCacheSyncState: function (id) {
    var state = HealthVideoStorage.results[id];
    if (state !== 0) delete HealthVideoStorage.results[id];
    return state === undefined ? -1 : state;
  }
});
