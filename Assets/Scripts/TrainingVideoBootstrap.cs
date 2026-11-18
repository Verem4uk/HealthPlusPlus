using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TrainingVideoBootstrap : MonoBehaviour
{
    [SerializeField]
    private Controller Controller;

    private VideoLoader Loader;
    private TrainingVideoCoordinator Coordinator;
    private CancellationTokenSource Preparation;
    private int PreparingLevel = -1;
    private Task PreparationTask;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        PrepareLevel(Saver.GetLevel());
    }

    private void Initialize()
    {
        if (Loader != null) return;
        Loader = new VideoLoader(Path.Combine(Application.persistentDataPath, "ExerciseVideos"));
        Coordinator = new TrainingVideoCoordinator(Controller.TrainingLevels, Loader);
    }

    public void PrepareLevel(int levelIndex)
    {
        Initialize();
        if (PreparingLevel == levelIndex && PreparationTask != null && !PreparationTask.IsCompleted) return;
        Preparation?.Cancel();
        Preparation?.Dispose();
        Preparation = new CancellationTokenSource();
        PreparingLevel = levelIndex;
        PreparationTask = PrepareAsync(levelIndex, Preparation.Token);
    }

    private async Task PrepareAsync(int levelIndex, CancellationToken cancellationToken)
    {
        try
        {
            await Coordinator.PrepareLevelAsync(levelIndex, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            Debug.LogWarning("Exercise video preparation failed: " + error.Message);
        }
    }

    public bool TryGetVideo(string url, out VideoSourceResult video)
    {
        Initialize();
        return Coordinator.TryGetVideo(url, out video);
    }

    private void OnDestroy()
    {
        Preparation?.Cancel();
        Preparation?.Dispose();
        Loader?.Dispose();
    }
}
