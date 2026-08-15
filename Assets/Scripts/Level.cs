using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Scriptable Objects/Level")]
public class Level : ScriptableObject
{
    [SerializeField]
    public int ID;

    [SerializeField]
    public ExerciseEntity[] Exercises;
}
