namespace DraftPuck.Shared.Achievements;

public class CheckAchievementsMessage
{
    public Guid UserId { get; set; }
    public AchievementTriggerType TriggerType { get; set; }
    public DateTime QueuedTimeUtc { get; set; } = DateTime.UtcNow;
}