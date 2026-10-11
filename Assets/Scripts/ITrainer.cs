
using System;

public interface ITrainer
{
    IExercise StartTrening(Level level, int baseNumber);
    IExercise NextSuccess();
    IExercise NextFail(int repeatsCompleted);
    bool NextLevelUnlocked();
    event Action<int> OnTrainingCompleted;
    int CheckRegression();
}
