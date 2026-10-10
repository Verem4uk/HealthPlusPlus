using UnityEngine;

public class ExerciseEntity : IExercise
{
    ExerciseSOEntity SOEntity;
    private int CurrentAmount;

    public ExerciseEntity(ExerciseSOEntity so, int baseNumber)
    {
        SOEntity = so;
        CurrentAmount = (int)(SOEntity.InWarmingMode ? 
            SOEntity.TargetAmount : baseNumber * SOEntity.Coefficient);
    }

    public Sprite GetImage() => SOEntity.GetIcon();
    public bool IsFullyCompleted() => CurrentAmount == SOEntity.TargetAmount;
    public int GetCurrentAmount() => CurrentAmount;    
}
