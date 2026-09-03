using UnityEngine;

public class ExerciseEntity : IExercise
{
    ExerciseSOEntity SOEntity;
    public int CurrentAmount { private set; get; }

    public ExerciseEntity(ExerciseSOEntity so, int currentAmount)
    {
        SOEntity = so;
        CurrentAmount = currentAmount;
    }

    public Sprite GetImage() => SOEntity.GetIcon();

    public bool IsFullyCompleted() => CurrentAmount == SOEntity.TargetAmount;

}
