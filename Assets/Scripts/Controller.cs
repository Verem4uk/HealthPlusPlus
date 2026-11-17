using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    [SerializeField]
    private GameObject StartScreen;

    [SerializeField]
    private GameObject TreningScreen;

    [SerializeField]
    private ExerciseTimerView ExerciseView;

    [SerializeField]
    private Image Background;

    [SerializeField]
    private RegressPanelView RegresPanel;

    [SerializeField]
    private Level[] Levels;

    private ITrainer Trainer;
    private int RequiredRepeats;

    private enum TrainingState
    {
        Idle,
        Training,
        AwaitingRegression
    }

    private TrainingState State;

    public void Play()
    {
        if (State != TrainingState.Idle)
        {
            return;
        }

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

        State = TrainingState.Training;
        ShowExe(exe);
    }

    private void ShowExe(IExercise exe)
    {
        if(exe == null)
        {            
            return;
        }

        RequiredRepeats = exe.GetCurrentAmount();
        ExerciseView.Show(exe.IsTimed, RequiredRepeats);
        Background.sprite = exe.GetImage();
    }  

    public void Next()
    {
        if (State != TrainingState.Training || !ExerciseView.CanComplete)
        {
            return;
        }

        var exe = Trainer.NextSuccess();
        ShowExe(exe);
    }

    public void Fail()
    {
        if (State != TrainingState.Training || !ExerciseView.CanFail)
        {
            return;
        }

        State = TrainingState.AwaitingRegression;
        var maxRepeats = Mathf.Max(0, RequiredRepeats - 1);
        if (ExerciseView.IsTimed)
        {
            var secondsCompleted = ExerciseView.StopCountdown();
            RegresPanel.Open(maxRepeats, secondsCompleted);
        }
        else
        {
            RegresPanel.Open(maxRepeats);
        }
    }

    public void NextAfterFail()
    {
        if (State != TrainingState.AwaitingRegression || !RegresPanel.IsOpen)
        {
            return;
        }

        var repeatsCompleted = RegresPanel.SelectedRepeats;
        State = TrainingState.Training;
        RegresPanel.Close();
        var exe = Trainer.NextFail(repeatsCompleted);
        ShowExe(exe);
    }

    public void OnCompleted(int baseNumber)
    {
        if (State != TrainingState.Training)
        {
            return;
        }

        State = TrainingState.Idle;
        Trainer.OnTrainingCompleted -= OnCompleted;

        StartScreen.SetActive(true);
        TreningScreen.SetActive(false);

        if (Trainer.NextLevelUnlocked() && Saver.GetLevel() < Levels.Length - 1)
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
