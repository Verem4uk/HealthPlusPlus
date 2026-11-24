using UnityEngine;
using System.Collections.Generic;

public class Controller : MonoBehaviour
{
    [SerializeField]
    private GameObject StartScreen;

    [SerializeField]
    private MainMenuTransitionView MainMenuView;

    [SerializeField]
    private GameObject TreningScreen;

    [SerializeField]
    private ExerciseTimerView ExerciseView;

    [SerializeField]
    private ExerciseMediaView MediaView;

    [SerializeField]
    private TrainingVideoBootstrap Videos;

    [SerializeField]
    private RegressPanelView RegresPanel;

    [SerializeField]
    private Level[] Levels;

    public IReadOnlyList<Level> TrainingLevels => Levels;

    private ITrainer Trainer;
    private int RequiredRepeats;
    private int TrainingLevelIndex;
    private double TrainingStartedAt;

    private enum TrainingState
    {
        Idle,
        Training,
        AwaitingRegression
    }

    private TrainingState State;

    public void Play()
    {
        if (!CloudProgress.IsReady || State != TrainingState.Idle || !Saver.IsTrainingAvailable() || !MainMenuView.CanStartTraining)
        {
            return;
        }

        StartScreen.SetActive(false);
        TreningScreen.SetActive(true);

        Trainer = new Trainer();

        Trainer.OnTrainingCompleted += OnCompleted;

        var lvlIndex = Saver.GetLevel();
        Videos.PrepareLevel(lvlIndex);
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
        TrainingLevelIndex = lvlIndex;
        TrainingStartedAt = Time.realtimeSinceStartupAsDouble;
        ProductAnalytics.TrainingStart(lvlIndex, baseNumber);
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
        Videos.TryGetVideo(exe.VideoUrl, out var video);
        MediaView.Show(exe.GetImage(), video);
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

        if (Trainer.NextLevelUnlocked() && Saver.GetLevel() < Levels.Length - 1)
        {
            Saver.LevelUp();
        }
        else
        {
            var regression = Trainer.CheckRegression();
            if (regression > 0)
            {
                Saver.SetBaseNumber(regression);
                Debug.Log("Base number after training is: " + regression);
            }
            else
            {
                Debug.Log("Base number after training is: " + baseNumber);
                Saver.SetBaseNumber(++baseNumber);
            }
        }

        Saver.MarkTrainingCompleted();
        CloudProgress.Save();
        ProductAnalytics.TrainingComplete(TrainingLevelIndex, Time.realtimeSinceStartupAsDouble - TrainingStartedAt);
        Videos.PrepareLevel(Saver.GetLevel());
        TreningScreen.SetActive(false);
        StartScreen.SetActive(true);
        MainMenuView.PlayCompletionTransition();
    }
}
