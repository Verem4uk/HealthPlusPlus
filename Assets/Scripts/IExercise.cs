using UnityEngine;

public interface IExercise
{
    public int GetCurrentAmount();
    public bool IsFullyCompleted();
    public Sprite GetImage();
}
