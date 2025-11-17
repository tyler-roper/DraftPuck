namespace DraftPuck.Application.Features.Achievements.Rules;
public abstract class BaseAchievementRule(ILogger logger) : IAchievementRule
{
    protected readonly ILogger Logger = logger;
    public abstract string UniqueIdentifier { get; }
    public abstract IReadOnlyCollection<AchievementTriggerType> Triggers { get; }
    protected abstract Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user);

    public async Task<bool> IsCriteriaMetAsync(object contextObj)
    {
        if (contextObj is not AchievementContext context)
        {
            Logger.LogError("Invalid context type passed to {RuleName}", GetType().Name);
            return false;
        }

        try
        {
            return await IsCriteriaMetAsync(context.DbContext, context.User);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during criteria check for {UniqueIdentifier}", UniqueIdentifier);
            return false;
        }
    }
}