namespace DraftPuck.Core.Models;

public class GoalSummary
{
    public Player Player { get; set; } = null!;
    public string PeriodTime { get; set; } = null!;

    public bool IsSameGoal(GoalSummary goalSummary)
    {
        bool isSamePlayer = Player.Id == goalSummary.Player.Id;
        bool isSameTime = Math.Abs(GetPeriodTimeInSeconds(PeriodTime) - GetPeriodTimeInSeconds(goalSummary.PeriodTime)) <= 3;
        return isSamePlayer && isSameTime;
    }

    private static int GetPeriodTimeInSeconds(string periodTime)
    {
        string[] parts = periodTime.Split(':');
        int minutes = int.Parse(parts[0]);
        int seconds = int.Parse(parts[1]);

        return (minutes * 60) + seconds;
    }
}
