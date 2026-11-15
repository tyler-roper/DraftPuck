namespace DraftPuck.Application.Features.Achievements;

public class AchievementAwardService(IDbContext dbContext, IEnumerable<IAchievementRule> allRules, ILogger<AchievementAwardService> logger, IUserRepository userRepository)
{
    private readonly IReadOnlyDictionary<string, IAchievementRule> _rules = allRules.ToDictionary(rule => rule.UniqueIdentifier, StringComparer.OrdinalIgnoreCase);

    public async Task<List<AchievementEntity>> CheckAndAwardAllAsync(Guid userId, AchievementTriggerType triggerType, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetById(userId, cancellationToken);

        // Bots play in lobbies, but achievements are for people.
        if (user == null || user.IsBot) return [];

        var owned = user.UserAchievements
            .Select(ua => ua.Achievement.UniqueIdentifier)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var context = new AchievementContext { User = user, DbContext = dbContext };
        var newlyAwarded = new List<AchievementEntity>();

        foreach (var rule in _rules.Values.Where(rule => rule.Triggers.Contains(triggerType) && !owned.Contains(rule.UniqueIdentifier)))
        {
            if (!await rule.IsCriteriaMetAsync(context))
                continue;

            var achievement = await TryAwardAsync(user, rule, cancellationToken);
            if (achievement != null)
                newlyAwarded.Add(achievement);
        }

        return newlyAwarded;
    }

    /// <summary>
    /// Awards the achievement and its banner and title, saved on their own. Checks for the same user run in parallel
    /// from the queue (two quick goals queue two checks); if another check awarded it first, the unique index rejects
    /// this copy and it's not reported as new.
    /// </summary>
    private async Task<AchievementEntity?> TryAwardAsync(UserEntity user, IAchievementRule rule, CancellationToken ct)
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

        var added = new List<object>
        {
            new UserAchievementEntity { UserId = user.Id, AchievementId = achievement.Id, DateEarned = DateTime.UtcNow }
        };

        if (achievement.Banners.FirstOrDefault() is { } banner && user.UserBanners.All(ub => ub.BannerId != banner.Id))
            added.Add(new UserBannerEntity { UserId = user.Id, BannerId = banner.Id, IsEquipped = user.UserBanners.Count == 0 });

        if (achievement.Titles.FirstOrDefault() is { } title && user.UserTitles.All(ut => ut.TitleId != title.Id))
            added.Add(new UserTitleEntity { UserId = user.Id, TitleId = title.Id, IsEquipped = user.UserTitles.Count == 0 });

        foreach (var entity in added)
            dbContext.Entry(entity).State = EntityState.Added;

        try
        {
            await dbContext.SaveChangesAsync(ct);
            return achievement;
        }
        catch (DbUpdateException)
        {
            foreach (var entity in added)
                dbContext.Entry(entity).State = EntityState.Detached;

            var alreadyAwarded = await dbContext.UserAchievements.AnyAsync(ua => ua.UserId == user.Id && ua.AchievementId == achievement.Id, ct);
            if (!alreadyAwarded) throw;

            logger.LogInformation("Achievement {Identifier} was already awarded to {UserId} by a concurrent check.", rule.UniqueIdentifier, user.Id);
            return null;
        }
    }
}
