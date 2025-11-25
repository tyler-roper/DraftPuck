namespace DraftPuck.Application.Features.Achievements;

public class AchievementAwardService(IDbContext dbContext, IEnumerable<IAchievementRule> allRules, ILogger<AchievementAwardService> logger, IUserRepository userRepository)
{
    private readonly IReadOnlyDictionary<string, IAchievementRule> _rules = allRules.ToDictionary(rule => rule.UniqueIdentifier, StringComparer.OrdinalIgnoreCase);

    public async Task<List<AchievementEntity>> CheckAndAwardAllAsync(Guid userId, AchievementTriggerType triggerType, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(userId, cancellationToken);
        if (user == null) return [];

        var context = new AchievementContext { User = user, DbContext = dbContext };
        var newlyAwarded = new List<AchievementEntity>();

        var relevantRules = _rules.Values
            .Where(rule => rule.Triggers.Contains(triggerType))
            .ToList();

        foreach (var rule in relevantRules)
        {
            var isAchievementOwned = user.UserAchievements
                .Any(ua => ua.Achievement.UniqueIdentifier == rule.UniqueIdentifier);

            if (isAchievementOwned) continue;

            if (await rule.IsCriteriaMetAsync(context))
            {
                var achievement = await AwardAchievementBundle(user, rule, cancellationToken);
                if (achievement != null)
                    newlyAwarded.Add(achievement);
            }
        }

        if (newlyAwarded.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return newlyAwarded;
    }


    private async Task<AchievementEntity?> AwardAchievementBundle(UserEntity user, IAchievementRule rule, CancellationToken ct)
    {
        var achievement = await dbContext.Achievements
            .Include(a => a.Banners)
            .Include(a => a.Titles)
            .FirstOrDefaultAsync(a => a.UniqueIdentifier == rule.UniqueIdentifier, ct);

        if (achievement == null)
        {
            logger.LogError("Achievement not found for identifier: {Identifier}", rule.UniqueIdentifier);
            return null;
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

        //Saving changes is handled by the caller

        return achievement;
    }

}