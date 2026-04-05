using System.Text.Json.Serialization;

namespace Shared.Models;

public class Record
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public string Display => Ties > 0 ? $"{Wins}-{Losses}-{Ties}" : $"{Wins}-{Losses}";
    public string PointsFor { get; set; } = "0";
}

public class WeekRecord
{
    public WeekRecord(int weekNumber)
    {
        WeekNumber = weekNumber;
    }

    public int WeekNumber { get; }
    public Dictionary<int, Record> ByRoster { get; } = new();
}

public class SeasonRecord
{
    public SeasonRecord(string season)
    {
        Season = season;
    }

    public string Season { get; }
    public Dictionary<int, WeekRecord> WeeksByNumber { get; } = new();
}
