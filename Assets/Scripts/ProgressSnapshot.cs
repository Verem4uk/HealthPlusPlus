using System;
using System.Globalization;

[Serializable]
public sealed class ProgressSnapshot
{
    public int schemaVersion = 1;
    public int level;
    public int baseNumber;
    public string completedTrainingDay = "";

    public bool IsValid(int levelCount)
    {
        return schemaVersion == 1 && level >= 0 && level < levelCount &&
            baseNumber >= 0 && baseNumber <= 1000000 && completedTrainingDay != null &&
            (completedTrainingDay.Length == 0 || DateTime.TryParseExact(completedTrainingDay,
                "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _));
    }
}
