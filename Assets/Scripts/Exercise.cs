using UnityEngine;

[CreateAssetMenu(fileName = "Exercise", menuName = "Scriptable Objects/Exercise")]
public class Exercise : ScriptableObject
{

    [SerializeField]
    public Sprite Icon;

    [SerializeField, TextAreaAttribute]
    public string RussianText;

    [SerializeField, TextAreaAttribute]
    public string EnglishText;

    [SerializeField, TextAreaAttribute]
    public string PolishText;
    public string GetText()
    {
        var languageIndex = Root.GetLanguageIndex();
        return languageIndex switch
        {
            1 => PolishText,
            2 => RussianText,
            _ => EnglishText
        };
    }
}
