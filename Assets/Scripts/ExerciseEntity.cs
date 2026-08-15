using System;
using UnityEngine;

[Serializable]
public class ExerciseEntity
{
    [SerializeField] 
    private Exercise exercise;
    [SerializeField] 
    private int targetAmount;

    public Exercise Exercise => exercise;
    public int TargetAmount => targetAmount;
}
