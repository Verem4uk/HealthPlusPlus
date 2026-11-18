using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Called on Unity's main thread. This service has no scene or training dependencies.
public sealed class VideoLoader : IVideoLoader, IDisposable
{
    private readonly string CacheDirectory;
    private readonly Dictionary<string, Task<bool>> Downloads = new();
    private readonly SemaphoreSlim DownloadSlot = new(1, 1);
    private readonly CancellationTokenSource Lifetime = new();
    private bool Disposed;

    public VideoLoader(string cacheDirectory)
    {
        CacheDirectory = cacheDirectory;
    }

    public bool IsCached(string sourceUrl)
    {
        return TryNormalizeUrl(sourceUrl, out var url) && IsVideoFile(GetCachePath(url));
    }

    public bool TryGetVideo(string sourceUrl, out VideoSourceResult video)
    {
        video = null;
        if (Disposed || !TryNormalizeUrl(sourceUrl, out var url))
        {
            return false;
        }

        var path = GetCachePath(url);
        if (!IsVideoFile(path))
        {
            return false;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        var blobUrl = HealthVideo_CreateBlobUrl(path);
        if (string.IsNullOrEmpty(blobUrl))
        {
            return false;
        }

        video = new VideoSourceResult(blobUrl, () => HealthVideo_ReleaseBlobUrl(blobUrl));
#else
        video = new VideoSourceResult(new Uri(Path.GetFullPath(path)).AbsoluteUri);
#endif
        return true;
    }

    public Task<bool> DownloadAsync(string sourceUrl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Disposed || !TryNormalizeUrl(sourceUrl, out var url))
        {
            return Task.FromResult(false);
        }

        if (IsCached(url))
        {
            return Task.FromResult(true);
        }

        if (!Downloads.TryGetValue(url, out var download))
        {
            var completion = new TaskCompletionSource<bool>();
            download = completion.Task;
            Downloads.Add(url, download);
            _ = CompleteDownloadAsync(url, completion);
        }

        return WaitForDownloadAsync(download, cancellationToken);
    }

    private async Task CompleteDownloadAsync(string url, TaskCompletionSource<bool> completion)
    {
        bool downloaded = false;
        bool entered = false;
        string temporaryPath = null;
        try
        {
            await DownloadSlot.WaitAsync(Lifetime.Token);
            entered = true;
            Lifetime.Token.ThrowIfCancellationRequested();
            var path = GetCachePath(url);
            if (IsVideoFile(path))
            {
                downloaded = true;
            }
            else
            {
                Directory.CreateDirectory(CacheDirectory);
                temporaryPath = path + ".part";
                DeleteTemporaryFile(temporaryPath);
                using (var request = UnityWebRequest.Get(url))
                {
                    request.timeout = 15;
#if !UNITY_WEBGL || UNITY_EDITOR
                    request.downloadHandler.Dispose();
                    request.downloadHandler = new DownloadHandlerFile(temporaryPath) { removeFileOnAbort = true };
#endif
                    var operation = request.SendWebRequest();
                    using (Lifetime.Token.Register(request.Abort))
                    {
                        while (!operation.isDone)
                        {
                            Lifetime.Token.ThrowIfCancellationRequested();
                            await Task.Yield();
                        }
                    }

                    Lifetime.Token.ThrowIfCancellationRequested();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning("Exercise video download failed: " + request.error);
                    }
                    else
                    {
#if UNITY_WEBGL && !UNITY_EDITOR
                        File.WriteAllBytes(temporaryPath, request.downloadHandler.data);
#endif
                        if (IsVideoFile(temporaryPath))
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }

                            File.Move(temporaryPath, path);
#if UNITY_WEBGL && !UNITY_EDITOR
                            await PersistWebCacheAsync(Lifetime.Token);
#endif
                            downloaded = true;
                        }
                        else
                        {
                            Debug.LogWarning("Exercise video response is not a valid MP4 file.");
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Application shutdown cancels the shared transfer.
        }
        catch (Exception error)
        {
            Debug.LogWarning("Exercise video could not be cached: " + error.Message);
        }
        finally
        {
            DeleteTemporaryFile(temporaryPath);
            if (entered)
            {
                DownloadSlot.Release();
            }

            Downloads.Remove(url);
            completion.TrySetResult(downloaded);
        }
    }

    private static async Task<bool> WaitForDownloadAsync(Task<bool> download, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            return await download;
        }

        var cancelled = new TaskCompletionSource<bool>();
        using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
        {
            await Task.WhenAny(download, cancelled.Task);
            cancellationToken.ThrowIfCancellationRequested();
            return await download;
        }
    }

    private string GetCachePath(string url)
    {
        using var hash = SHA256.Create();
        var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(url));
        var name = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        return Path.Combine(CacheDirectory, name + ".mp4");
    }

    private static bool TryNormalizeUrl(string value, out string url)
    {
        url = null;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        url = uri.AbsoluteUri;
        return true;
    }

    private static bool IsVideoFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            using var stream = File.OpenRead(path);
            if (stream.Length < 12)
            {
                return false;
            }

            // MP4 is an ISO base media container. Reject empty/error-page downloads.
            var header = new byte[(int)Math.Min(4096, stream.Length)];
            int length = stream.Read(header, 0, header.Length);
            for (int i = 4; i <= length - 4; i++)
            {
                if (header[i] == 'f' && header[i + 1] == 't' && header[i + 2] == 'y' && header[i + 3] == 'p')
                {
                    return true;
                }
            }

            return false;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static void DeleteTemporaryFile(string path)
    {
        if (path == null) return;
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public void Dispose()
    {
        if (Disposed) return;
        Disposed = true;
        Lifetime.Cancel();
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string HealthVideo_CreateBlobUrl(string path);

    [DllImport("__Internal")]
    private static extern void HealthVideo_ReleaseBlobUrl(string url);

    [DllImport("__Internal")]
    private static extern int HealthVideo_BeginCacheSync();

    [DllImport("__Internal")]
    private static extern int HealthVideo_GetCacheSyncState(int id);

    private static async Task PersistWebCacheAsync(CancellationToken cancellationToken)
    {
        int id = HealthVideo_BeginCacheSync();
        int state;
        while ((state = HealthVideo_GetCacheSyncState(id)) == 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }

        if (state < 0)
        {
            Debug.LogWarning("Video is ready, but the browser could not persist its cache for the next launch.");
        }
    }
#endif
}
