namespace DraftPuck.Application.Features.Achievements;

public class CheckAndAwardAllAchievementsCommandHandler(AchievementAwardService awardService) : IRequestHandler<CheckAndAwardAllAchievementsCommand>
{
    public async Task Handle(CheckAndAwardAllAchievementsCommand request, CancellationToken cancellationToken)
    {
        await awardService.CheckAndAwardAllAsync(
            request.UserId,
            request.TriggerType,
            cancellationToken
        );
    }
}