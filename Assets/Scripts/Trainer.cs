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

    public Action OnCompleted;

    public IExercise StartTrening(Level level, int baseNumber)
    {
        ExercisesLength = level.Exercises.Length;
        Level = level;

        var exeSO = Level.Exercises[0];
        CurrentExe = new ExerciseEntity(exeSO, baseNumber);

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
            OnCompleted.Invoke();
            return null;
        }

        var exeSO = Level.Exercises[ExerciseIndex];
        CurrentExe = new ExerciseEntity(exeSO, BaseNumber);

        return CurrentExe;
    }    

    public IExercise NextFail(int repeatsCompleted)
    {
        BaseNumber = repeatsCompleted;
        Regression = repeatsCompleted / 2;

        return NextSuccess();
    }

    public bool NextLevelUnlocked() => Regression == 0 && FullyCompleted;

}
