using UnityEngine;
using UnityEngine.UI;

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
    private NumbersView RegresNumbersView;

    [SerializeField]
    private Button RegresMinusButton;

    [SerializeField]
    private Button RegresPlusButton;

    [SerializeField]
    private Level[] Levels;

    private ITrainer Trainer;
    private int RequiredRepeats;
    private int RegresRepeats;
    private int RegresMaxRepeats;

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

        RequiredRepeats = exe.GetCurrentAmount();
        NumbersView.SetValue(RequiredRepeats);
        Background.sprite = exe.GetImage();
    }  

    public void Next()
    {
        var exe = Trainer.NextSuccess();
        ShowExe(exe);
    }

    public void Fail()
    {
        RegresMaxRepeats = Mathf.Max(0, RequiredRepeats - 1);
        RegresRepeats = RegresMaxRepeats;
        UpdateRegresCounter();
        RegresPanel.SetActive(true);
    }

    public void DecreaseRegresRepeats()
    {
        RegresRepeats = Mathf.Max(0, RegresRepeats - 1);
        UpdateRegresCounter();
    }

    public void IncreaseRegresRepeats()
    {
        RegresRepeats = Mathf.Min(RegresMaxRepeats, RegresRepeats + 1);
        UpdateRegresCounter();
    }

    private void UpdateRegresCounter()
    {
        RegresNumbersView.SetValue(RegresRepeats);
        RegresMinusButton.interactable = RegresRepeats > 0;
        RegresPlusButton.interactable = RegresRepeats < RegresMaxRepeats;
    }

    public void NextAfterFail()
    {
        RegresPanel.SetActive(false);
        var exe = Trainer.NextFail(RegresRepeats);
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
