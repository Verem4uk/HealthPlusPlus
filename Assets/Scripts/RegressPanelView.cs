using UnityEngine;
using UnityEngine.UI;

public class RegressPanelView : MonoBehaviour
{
    [SerializeField]
    private NumbersView NumbersView;

    [SerializeField]
    private Button MinusButton;

    [SerializeField]
    private Button PlusButton;

    private int MaxRepeats;
    private bool Opened;

    public int SelectedRepeats { get; private set; }
    public bool IsOpen => Opened && gameObject.activeSelf;

    public void Open(int maxRepeats)
    {
        Open(maxRepeats, maxRepeats);
    }

    public void Open(int maxRepeats, int initialRepeats)
    {
        MaxRepeats = Mathf.Max(0, maxRepeats);
        SelectedRepeats = Mathf.Clamp(initialRepeats, 0, MaxRepeats);
        Opened = true;
        UpdateCounter();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        Opened = false;
        gameObject.SetActive(false);
    }

    public void DecreaseRepeats()
    {
        if (!IsOpen)
        {
            return;
        }

        SelectedRepeats = Mathf.Max(0, SelectedRepeats - 1);
        UpdateCounter();
    }

    public void IncreaseRepeats()
    {
        if (!IsOpen)
        {
            return;
        }

        SelectedRepeats = Mathf.Min(MaxRepeats, SelectedRepeats + 1);
        UpdateCounter();
    }

    private void UpdateCounter()
    {
        NumbersView.SetValue(SelectedRepeats);
        MinusButton.interactable = SelectedRepeats > 0;
        PlusButton.interactable = SelectedRepeats < MaxRepeats;
    }
}
