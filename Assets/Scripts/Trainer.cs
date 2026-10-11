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

    public event Action<int> OnTrainingCompleted;

    public IExercise StartTrening(Level level, int baseNumber)
    {
        ExercisesLength = level.Exercises.Length;
        Level = level;
        BaseNumber = baseNumber;

        var exeSO = Level.Exercises[0];
        CurrentExe = new ExerciseEntity(exeSO, BaseNumber);

        return CurrentExe;
    }

    public IExercise NextSuccess()
    {
        if(!CurrentExe.IsFullyCompleted())
        {
            FullyCompleted = false;
        }

        ExerciseIndex++;
        if(ExerciseIndex > ExercisesLength - 1)
        {
            OnTrainingCompleted?.Invoke(BaseNumber);
            return null;
        }

        var exeSO = Level.Exercises[ExerciseIndex];
        CurrentExe = new ExerciseEntity(exeSO, BaseNumber);

        return CurrentExe;
    }    

    public IExercise NextFail(int repeatsCompleted)
    {
        BaseNumber = BaseNumber > repeatsCompleted ? repeatsCompleted : BaseNumber;
        Regression = repeatsCompleted / 2;

        return NextSuccess();
    }

    public bool NextLevelUnlocked() => Regression == 0 && FullyCompleted;

    public int CheckRegression() => Regression;

}
