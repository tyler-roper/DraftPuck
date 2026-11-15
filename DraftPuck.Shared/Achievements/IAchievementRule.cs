namespace DraftPuck.Shared.Achievements;

public interface IAchievementRule
{
    string UniqueIdentifier { get; }
    Task<bool> IsCriteriaMetAsync(object context);
    IReadOnlyCollection<AchievementTriggerType> Triggers { get; }

    /// <summary>
    /// False for a rule that trusts its trigger instead of checking stored data (it can only be judged when the event
    /// happens), so a backfill, which runs every rule, must skip it.
    /// </summary>
    bool CanBackfill { get; }
}