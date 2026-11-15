using DraftPuck.Shared.Firebase;

namespace DraftPuck.Application.Features.Achievements;

public class CheckAndAwardAllAchievementsCommandHandler(AchievementAwardService awardService, IPushNotificationService pushService, IDbContext dbContext) : IRequestHandler<CheckAndAwardAllAchievementsCommand>
{
    public async Task Handle(CheckAndAwardAllAchievementsCommand request, CancellationToken cancellationToken)
    {
        var awarded = await awardService.CheckAndAwardAllAsync(
            request.UserId,
            request.BackfillId == null ? request.TriggerType : null,
            cancellationToken
        );

        if (request.BackfillId is { } backfillId)
        {
            // Counted in one statement, so checks running in parallel don't overwrite each other's counts.
            await dbContext.AchievementBackfills
                .Where(b => b.Id == backfillId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.UsersChecked, b => b.UsersChecked + 1)
                    .SetProperty(b => b.AchievementsAwarded, b => b.AchievementsAwarded + awarded.Count), cancellationToken);

            // A backfill awards what people earned in the past; a burst of notifications for it would just be noise.
            return;
        }

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