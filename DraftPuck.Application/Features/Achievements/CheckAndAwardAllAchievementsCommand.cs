namespace DraftPuck.Application.Features.Achievements;

public class CheckAndAwardAllAchievementsCommand : IRequest
{
    public Guid UserId { get; set; }
    public AchievementTriggerType TriggerType { get; set; }
}