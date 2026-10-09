using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NumbersView : MonoBehaviour
{
    [Header("Digits from 0 to 9")]
    [SerializeField] 
    private Sprite[] digits = new Sprite[10];

    [Header("UI")]
    [SerializeField] 
    private Image digitPrefab;

    [SerializeField] 
    private RectTransform container;

    private readonly List<Image> images = new();

    public void SetValue(int value)
    {
        value = Mathf.Max(0, value);
        string text = value.ToString();

        EnsureImages(text.Length);

        for (int i = 0; i < images.Count; i++)
        {
            bool visible = i < text.Length;
            images[i].gameObject.SetActive(visible);

            if (!visible)
                continue;

            int digit = text[i] - '0';
            images[i].sprite = digits[digit];
            images[i].preserveAspect = true;
        }
    }

    private void EnsureImages(int count)
    {
        while (images.Count < count)
        {
            Image image = Instantiate(digitPrefab, container);
            images.Add(image);
        }
    }
}
