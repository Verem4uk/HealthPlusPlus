using UnityEngine;
using UnityEngine.UI;

public sealed class ExercisePresentation : MonoBehaviour
{
    public Text Title;
    public Text Unit;
    public Text Success;
    public Text Fail;
    public Text Start;
    public Text RegressionTitle;
    public Text Confirm;

    public void Show(string title, bool timed)
    {
        if (Title != null) Title.text = title;
        if (Unit != null) Unit.text = Localize(timed ? "SECONDS" : "REPETITIONS", timed ? "SEKUNDY" : "POWTÓRZENIA", timed ? "СЕКУНД" : "ПОВТОРЕНИЙ");
        RefreshLabels();
    }

    private void OnEnable() => RefreshLabels();
    public void RefreshLabels()
    {
        if (Success != null) Success.text = Localize("Done", "Gotowe", "Готово");
        if (Fail != null) Fail.text = Localize("Couldn't finish", "Nie udało się", "Не получилось");
        if (Start != null) Start.text = Localize("Start timer", "Uruchom timer", "Начать отсчёт");
        if (RegressionTitle != null) RegressionTitle.text = Localize("HOW MUCH DID YOU COMPLETE?", "ILE UDAŁO SIĘ WYKONAĆ?", "СКОЛЬКО ВЫПОЛНЕНО?");
        if (Confirm != null) Confirm.text = Localize("Confirm", "Potwierdź", "Подтвердить");
    }
    private static string Localize(string en, string pl, string ru) => Root.GetLanguageIndex() switch { 1 => pl, 2 => ru, _ => en };
}
