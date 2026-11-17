using System;
using UnityEngine;
using UnityEngine.UI;

public class ExerciseTimerView : MonoBehaviour
{
    [SerializeField]
    private NumbersView NumbersView;

    [SerializeField]
    private Button StartButton;

    [SerializeField]
    private Button SuccessButton;

    [SerializeField]
    private Button FailButton;

    private enum DisplayState
    {
        Repetitions,
        Ready,
        Running,
        Finished,
        Stopped
    }

    private DisplayState State;
    private int TotalSeconds;
    private double RemainingTime;
    private double LastTickTime;
    private int DisplayedValue = -1;
    private bool ApplicationPaused;
    private bool ApplicationFocused = true;

    public bool IsTimed => State != DisplayState.Repetitions;
    public bool CanComplete => State == DisplayState.Repetitions || State == DisplayState.Finished;
    public bool CanFail => State == DisplayState.Repetitions ||
        State == DisplayState.Running || State == DisplayState.Finished;
    public int RemainingSeconds => (int)Math.Ceiling(RemainingTime);
    public int ElapsedSeconds => TotalSeconds - RemainingSeconds;

    public void Show(bool timed, int amount)
    {
        TotalSeconds = Mathf.Max(0, amount);
        RemainingTime = TotalSeconds;
        LastTickTime = Time.realtimeSinceStartupAsDouble;
        State = timed ? DisplayState.Ready : DisplayState.Repetitions;
        DisplayNumber(TotalSeconds);
        UpdateButtons();
    }

    public void StartCountdown()
    {
        if (State != DisplayState.Ready)
        {
            return;
        }

        LastTickTime = Time.realtimeSinceStartupAsDouble;
        State = RemainingTime > 0 ? DisplayState.Running : DisplayState.Finished;
        UpdateButtons();
    }

    public int StopCountdown()
    {
        Tick();
        State = DisplayState.Stopped;
        UpdateButtons();
        return ElapsedSeconds;
    }

    private void Update()
    {
        Tick();
    }

    private void Tick()
    {
        var now = Time.realtimeSinceStartupAsDouble;
        if (State == DisplayState.Running && !ApplicationPaused && ApplicationFocused)
        {
            RemainingTime = Math.Max(0, RemainingTime - Math.Max(0, now - LastTickTime));
            DisplayNumber(RemainingSeconds);
            if (RemainingTime == 0)
            {
                State = DisplayState.Finished;
                UpdateButtons();
            }
        }

        LastTickTime = now;
    }

    private void OnApplicationPause(bool paused)
    {
        Tick();
        ApplicationPaused = paused;
    }

    private void OnApplicationFocus(bool focused)
    {
        Tick();
        ApplicationFocused = focused;
    }

    private void OnEnable()
    {
        LastTickTime = Time.realtimeSinceStartupAsDouble;
    }

    private void OnDisable()
    {
        Tick();
    }

    private void DisplayNumber(int value)
    {
        if (DisplayedValue == value)
        {
            return;
        }

        DisplayedValue = value;
        NumbersView.SetValue(value);
    }

    private void UpdateButtons()
    {
        StartButton.gameObject.SetActive(State == DisplayState.Ready);
        SuccessButton.gameObject.SetActive(CanComplete);
        FailButton.gameObject.SetActive(CanFail);
    }
}
