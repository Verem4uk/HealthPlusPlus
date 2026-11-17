using UnityEngine;

public interface IExercise
{
    bool IsTimed { get; }
    public int GetCurrentAmount();
    public bool IsFullyCompleted();
    public Sprite GetImage();
}
