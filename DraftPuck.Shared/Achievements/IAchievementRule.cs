namespace DraftPuck.Shared.Achievements;

public interface IAchievementRule
{
    string UniqueIdentifier { get; }
    Task<bool> IsCriteriaMetAsync(object context);
    IReadOnlyCollection<AchievementTriggerType> Triggers { get; }
}