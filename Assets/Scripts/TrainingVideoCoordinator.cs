using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class TrainingVideoCoordinator
{
    private readonly IReadOnlyList<Level> Levels;
    private readonly IVideoLoader Loader;

    public TrainingVideoCoordinator(IReadOnlyList<Level> levels, IVideoLoader loader)
    {
        Levels = levels;
        Loader = loader;
    }

    public IReadOnlyList<string> GetVideoUrls(int levelIndex)
    {
        var urls = new List<string>();
        if (levelIndex < 0 || levelIndex >= Levels.Count || Levels[levelIndex] == null)
        {
            return urls;
        }

        var exercises = Levels[levelIndex].Exercises;
        if (exercises == null)
        {
            return urls;
        }

        var seen = new HashSet<string>();
        foreach (var exercise in exercises)
        {
            var url = exercise?.VideoUrl;
            if (!string.IsNullOrWhiteSpace(url) && seen.Add(url))
            {
                urls.Add(url);
            }
        }

        return urls;
    }

    public async Task PrepareLevelAsync(int levelIndex, CancellationToken cancellationToken)
    {
        foreach (var url in GetVideoUrls(levelIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Loader.IsCached(url))
            {
                continue;
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (await Loader.DownloadAsync(url, cancellationToken))
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

    public bool TryGetVideo(string sourceUrl, out VideoSourceResult video) =>
        Loader.TryGetVideo(sourceUrl, out video);
}
