using UnityEngine;

public interface IExercise
{
    bool IsTimed { get; }
    string VideoUrl { get; }
    string DisplayName { get; }
    public int GetCurrentAmount();
    public bool IsFullyCompleted();
    public Sprite GetImage();
}
