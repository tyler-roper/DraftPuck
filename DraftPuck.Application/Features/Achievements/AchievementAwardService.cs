namespace DraftPuck.Application.Features.Achievements;

public class AchievementAwardService(
    IDbContext dbContext,
    IEnumerable<IAchievementRule> allRules,
    ILogger<AchievementAwardService> logger,
    IUserRepository userRepository)
{
    private readonly IReadOnlyDictionary<string, IAchievementRule> _rules = allRules.ToDictionary(rule => rule.UniqueIdentifier, StringComparer.OrdinalIgnoreCase);

    public async Task CheckAndAwardAllAsync(Guid userId, AchievementTriggerType triggerType, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(userId, cancellationToken);
        if (user == null) return;

        var context = new AchievementContext { User = user, DbContext = dbContext };
        var awardedNewItem = false;

        var relevantRules = _rules.Values
            .Where(rule => rule.Triggers.Contains(triggerType))
            .ToList();

        foreach (var rule in relevantRules)
        {
            var isAchievementOwned = user.UserAchievements.Any(ua => ua.Achievement.UniqueIdentifier == rule.UniqueIdentifier);
            if (isAchievementOwned) continue;

            if (await rule.IsCriteriaMetAsync(context))
            {
                await AwardAchievementBundle(user, rule, cancellationToken);
                awardedNewItem = true;
            }
        }

        if (awardedNewItem)
            await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AwardAchievementBundle(UserEntity user, IAchievementRule rule, CancellationToken ct)
    {
        var achievement = await dbContext.Achievements
            .Include(a => a.Banners)
            .Include(a => a.Titles)
            .FirstOrDefaultAsync(a => a.UniqueIdentifier == rule.UniqueIdentifier, ct);

        if (achievement == null)
        {
            logger.LogError("Achievement not found for identifier: {Identifier}", rule.UniqueIdentifier);
            return;
        }

        dbContext.UserAchievements.Add(new UserAchievementEntity
        {
            UserId = user.Id,
            AchievementId = achievement.Id,
            DateEarned = DateTime.UtcNow
        });

        var bannerReward = achievement.Banners.FirstOrDefault();
        if (bannerReward != null)
        {
            dbContext.UserBanners.Add(new UserBannerEntity
            {
                UserId = user.Id,
                BannerId = bannerReward.Id,
                IsEquipped = user.UserBanners.Count == 0
            });
        }

        var titleReward = achievement.Titles.FirstOrDefault();
        if (titleReward != null)
        {
            dbContext.UserTitles.Add(new UserTitleEntity
            {
                UserId = user.Id,
                TitleId = titleReward.Id,
                IsEquipped = user.UserTitles.Count == 0
            });
        }

        // No SaveChangesAsync here. The calling method handles the commit.
    }
}