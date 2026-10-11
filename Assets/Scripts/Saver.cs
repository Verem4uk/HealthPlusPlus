using UnityEngine;

public static class Saver
{    
    public static void SetLanguage(int value) => PlayerPrefs.SetInt("Language", value);
    public static int GetLanguage() => PlayerPrefs.GetInt("Language");

    public static void SetBaseNumber(int value) => PlayerPrefs.SetInt("BaseNumber", value);
    public static int GetBaseNumber() => PlayerPrefs.GetInt("BaseNumber");

    public static void LevelUp()
    {
        var currentLvl = GetLevel();
        SetLevel(++currentLvl);
        SetBaseNumber(0);
    }

    public static void SetLevel(int value)
    {
        var savedIndex = GetLevel();
        if(value > savedIndex)
        {
            PlayerPrefs.SetInt("Level", value);
        }        
    }

    public static int GetLevel() => PlayerPrefs.GetInt("Level");         
    
    public static void Clear()
    {
        PlayerPrefs.DeleteAll();
    }
}
