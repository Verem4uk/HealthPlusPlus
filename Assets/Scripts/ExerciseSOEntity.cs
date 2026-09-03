using System;
using UnityEngine;

[Serializable]
public class ExerciseSOEntity
{
    [SerializeField] 
    private ExerciseSO exercise;
    [SerializeField] 
    private int targetAmount;
       
    public int TargetAmount => targetAmount;

    public Sprite GetIcon() => exercise.Icon;
}
