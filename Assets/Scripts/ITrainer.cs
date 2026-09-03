
public interface ITrainer
{
    public IExercise StartTrening(Level level, int baseNumber);
    public IExercise NextSuccess();
    public IExercise NextFail(int repeatsCompleted);
}
