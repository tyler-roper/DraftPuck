namespace DraftPuck.Application.Features.Achievements;

public class CheckAchievementsMessage
{
    public Guid UserId { get; set; }
    public AchievementTriggerType TriggerType { get; set; }
    public DateTime QueuedTimeUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Set for an admin backfill (its run): check every rule, not just the trigger's, and send no push notifications.</summary>
    public Guid? BackfillId { get; set; }
}