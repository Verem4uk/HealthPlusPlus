using System;
using UnityEngine;

[Serializable]
public class ExerciseSOEntity
{
    [SerializeField] 
    private ExerciseSO exercise;
    [SerializeField] 
    private int targetAmount;
    [SerializeField]
    private bool inWarmingMode;
    [SerializeField]
    private float coefficient;
    [SerializeField]
    private bool evenOnly;

    public bool EvenOnly => evenOnly;

    public float Coefficient => coefficient;

    public int TargetAmount => targetAmount;

    public Sprite GetIcon() => exercise.Icon;
}
