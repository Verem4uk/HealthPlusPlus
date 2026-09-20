using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Controller : MonoBehaviour
{
    [SerializeField]
    private GameObject StartScreen;

    [SerializeField]
    private GameObject TreningScreen;

    [SerializeField]
    private TextMeshProUGUI Aim;

    [SerializeField]
    private Image Background;

    [SerializeField]
    private GameObject RegresPanel;

    [SerializeField]
    private TMP_InputField RegresInput;

    [SerializeField]
    private Level[] Levels;

    private ITrainer Trainer;

    public void Play()
    {
        StartScreen.SetActive(false);
        TreningScreen.SetActive(true);

        Trainer = new Trainer();

        var exe = Trainer.StartTrening(Levels[0], 20);
        ShowExe(exe);
    }

    private void ShowExe(IExercise exe)
    {
        if(exe == null)
        {
            StartScreen.SetActive(true);
            TreningScreen.SetActive(false);
        }

        Aim.text = exe.GetCurrentAmount().ToString();
        Background.sprite = exe.GetImage();
    }

    public void Next()
    {
        var exe = Trainer.NextSuccess();
        ShowExe(exe);
    }

    public void Fail()
    {
        RegresPanel.SetActive(true);
    }

    public void NextAfterFail()
    {
        int.TryParse(RegresInput.text, out int repeats);
        var exe = Trainer.NextFail(repeats);
        ShowExe(exe);
    }
}
