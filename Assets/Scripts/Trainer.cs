using System;

public class Trainer : ITrainer
{
    private int ExercisesLength;
    private int ExerciseIndex;
    private int BaseNumber;
    private Level Level;

    private int Regression;
    private bool FullyCompleted = true;

    private IExercise CurrentExe;
    private bool IsRunning;

    public event Action<int> OnTrainingCompleted;

    public IExercise StartTrening(Level level, int baseNumber)
    {
        ExercisesLength = level.Exercises.Length;
        Level = level;
        BaseNumber = baseNumber;

        var exeSO = Level.Exercises[0];
        CurrentExe = new ExerciseEntity(exeSO, BaseNumber);
        IsRunning = true;

        return CurrentExe;
    }

    public IExercise NextSuccess()
    {
        if (!IsRunning)
        {
            return null;
        }

        if(!CurrentExe.IsFullyCompleted())
        {
            FullyCompleted = false;
        }

        ExerciseIndex++;
        if(ExerciseIndex > ExercisesLength - 1)
        {
            IsRunning = false;
            OnTrainingCompleted?.Invoke(BaseNumber);
            return null;
        }

        var exeSO = Level.Exercises[ExerciseIndex];
        CurrentExe = new ExerciseEntity(exeSO, BaseNumber);

        return CurrentExe;
    }    

    public IExercise NextFail(int repeatsCompleted)
    {
        if (!IsRunning)
        {
            return null;
        }

        BaseNumber = BaseNumber > repeatsCompleted ? repeatsCompleted : BaseNumber;
        Regression = repeatsCompleted / 2;

        return NextSuccess();
    }

    public bool NextLevelUnlocked() => Regression == 0 && FullyCompleted;

    public int CheckRegression() => Regression;

}
