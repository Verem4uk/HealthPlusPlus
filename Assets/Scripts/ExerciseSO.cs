using UnityEngine;

[CreateAssetMenu(fileName = "Exercise", menuName = "Scriptable Objects/Exercise")]
public class ExerciseSO : ScriptableObject
{
    [SerializeField]
    private bool timed;

    public bool IsTimed => timed;

    [SerializeField, Tooltip("HTTPS URL of the exercise MP4. Leave empty to use the image.")]
    private string videoUrl;

    public string VideoUrl => videoUrl?.Trim() ?? string.Empty;

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
        string text = languageIndex switch
        {
            1 => PolishText,
            2 => RussianText,
            _ => EnglishText
        };
        return string.IsNullOrWhiteSpace(text) ? EnglishText : text;
    }
}
