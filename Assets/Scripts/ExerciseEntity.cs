using UnityEngine;

public class ExerciseEntity : IExercise
{
    ExerciseSOEntity SOEntity;
    private int CurrentAmount;

    public ExerciseEntity(ExerciseSOEntity so, int currentAmount)
    {
        SOEntity = so;
        CurrentAmount = currentAmount;
    }

    public Sprite GetImage() => SOEntity.GetIcon();
    public bool IsFullyCompleted() => CurrentAmount == SOEntity.TargetAmount;
    public int GetCurrentAmount() => CurrentAmount;    
}
