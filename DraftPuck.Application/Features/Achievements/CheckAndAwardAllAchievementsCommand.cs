namespace DraftPuck.Application.Features.Achievements;

public class CheckAndAwardAllAchievementsCommand : IRequest
{
    public Guid UserId { get; set; }
    public AchievementTriggerType TriggerType { get; set; }

    /// <summary>Set for an admin backfill: check every rule regardless of trigger, award silently, and count it toward the run.</summary>
    public Guid? BackfillId { get; set; }
}
