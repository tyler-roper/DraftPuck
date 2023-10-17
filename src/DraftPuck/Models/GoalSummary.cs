using DraftPuck.Models.NhlApi;

namespace DraftPuck.Models
{
    public class GoalSummary
    {
        public PlayerSummary Player { get; set; } = null!;
        public string PeriodTime { get; set; } = null!;

        public bool IsSameGoal(GoalSummary goalSummary)
        {
            var isSamePlayer = Player.Id == goalSummary.Player.Id;
            var isSameTime = Math.Abs(GetPeriodTimeInSeconds(PeriodTime) - GetPeriodTimeInSeconds(goalSummary.PeriodTime)) <= 3;
            return isSamePlayer && isSameTime;
        }

        private static int GetPeriodTimeInSeconds(string periodTime)
        {
            var parts = periodTime.Split(':');
            var minutes = int.Parse(parts[0]);
            var seconds = int.Parse(parts[1]);

            return (minutes * 60) + seconds;
        }
    }
}
