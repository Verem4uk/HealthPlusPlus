
public interface ITrainer
{
    public IExercise StartTrening(Level level);
    public IExercise NextSuccess();
    public IExercise NextFail(int repeatsCompleted);
}
