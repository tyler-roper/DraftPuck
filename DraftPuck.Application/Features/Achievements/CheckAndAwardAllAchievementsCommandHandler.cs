using DraftPuck.Shared.Firebase;

namespace DraftPuck.Application.Features.Achievements;

public class CheckAndAwardAllAchievementsCommandHandler(AchievementAwardService awardService, IPushNotificationService pushService, IDbContext dbContext) : IRequestHandler<CheckAndAwardAllAchievementsCommand>
{
    public async Task Handle(CheckAndAwardAllAchievementsCommand request, CancellationToken cancellationToken)
    {
        var awarded = await awardService.CheckAndAwardAllAsync(
            request.UserId,
            request.TriggerType,
            cancellationToken
        );

        if (awarded.Count == 0) return;

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null
            || string.IsNullOrEmpty(user.FcmRegistrationToken)
            || user.AchievementAwardedNotificationPreference == NotificationPreference.None)
            return;

        foreach (var achievement in awarded)
        {
            await pushService.SendAchievementNotification(
                user.FcmRegistrationToken,
                achievement,
                cancellationToken
            );
        }
    }
}