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
    private NumbersView NumbersView;

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

        Trainer.OnTrainingCompleted += OnCompleted;

        var lvlIndex = Saver.GetLevel();
        var baseNumber = Saver.GetBaseNumber();
        Debug.Log("Start base number is: " + baseNumber);
        var level = Levels[lvlIndex];

        if(baseNumber == 0)
        {
            baseNumber = level.BaseNumer;
            Debug.Log("Base number from lvl is: " + baseNumber);
        }

        var exe = Trainer.StartTrening(Levels[lvlIndex], baseNumber);

        ShowExe(exe);
    }

    private void ShowExe(IExercise exe)
    {
        if(exe == null)
        {            
            return;
        }

        NumbersView.SetValue(exe.GetCurrentAmount());
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
        RegresPanel.SetActive(false);
        int.TryParse(RegresInput.text, out int repeats);
        var exe = Trainer.NextFail(repeats);
        ShowExe(exe);
    }

    public void OnCompleted(int baseNumber)
    {
        Trainer.OnTrainingCompleted -= OnCompleted;

        StartScreen.SetActive(true);
        TreningScreen.SetActive(false);

        if (Trainer.NextLevelUnlocked())
        {
            Saver.LevelUp();
            return;
        }

        var regression = Trainer.CheckRegression();
        if (regression > 0)
        {
            Saver.SetBaseNumber(regression);
            Debug.Log("Base number after training is: " + regression);
            return;
        }

        Debug.Log("Base number after training is: " + baseNumber);
        Saver.SetBaseNumber(++baseNumber);        
    }
}
