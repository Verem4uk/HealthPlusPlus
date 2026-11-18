using System.Threading;
using System.Threading.Tasks;

public interface IVideoLoader
{
    bool IsCached(string sourceUrl);
    bool TryGetVideo(string sourceUrl, out VideoSourceResult video);
    Task<bool> DownloadAsync(string sourceUrl, CancellationToken cancellationToken);
}
