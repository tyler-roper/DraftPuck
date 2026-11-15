using DraftPuck.Application.Features.Achievements;

namespace DraftPuck.Application.Features.Admin.Achievements;

/// <summary>A backfill run and how far along it is.</summary>
public class AchievementBackfillDto
{
    public Guid Id { get; set; }
    public DateTime StartedUtc { get; set; }
    public int UsersQueued { get; set; }
    public int UsersChecked { get; set; }
    public int AchievementsAwarded { get; set; }
    public bool IsComplete => UsersChecked >= UsersQueued;

    public static AchievementBackfillDto From(AchievementBackfillEntity run) => new()
    {
        Id = run.Id,
        StartedUtc = run.StartedUtc,
        UsersQueued = run.UsersQueued,
        UsersChecked = run.UsersChecked,
        AchievementsAwarded = run.AchievementsAwarded
    };
}

/// <summary>
/// Awards achievements people earned before their rules existed or were fixed. Records a run, then queues one silent
/// check of every rule for each person who has played (been in a lobby) or linked Discord; the worker does the
/// checking and counts each one toward the run. Safe to run again: owned achievements are skipped, and the unique
/// indexes stop duplicates.
/// </summary>
public class BackfillAchievementsCommand : IRequest<AchievementBackfillDto>
{
    public Guid StartedBy { get; set; }
}

/// <summary>The most recent backfill run, or null if there's never been one.</summary>
public class GetLatestAchievementBackfillQuery : IRequest<AchievementBackfillDto?> { }

public class BackfillAchievementsCommandHandler(IDbContext dbContext, IAchievementQueueService queueService, ILogger<BackfillAchievementsCommandHandler> logger)
    : IRequestHandler<BackfillAchievementsCommand, AchievementBackfillDto>,
      IRequestHandler<GetLatestAchievementBackfillQuery, AchievementBackfillDto?>
{
    public async Task<AchievementBackfillDto> Handle(BackfillAchievementsCommand request, CancellationToken ct)
    {
        var userIds = await dbContext.Users
            .Where(u => !u.IsBot && u.IsActive && (u.DiscordUserId != null || u.LobbyMembers.Any(lm => !lm.IsBot)))
            .Select(u => u.Id)
            .ToListAsync(ct);

        // Saved before queueing, so the first checks have a run to count toward.
        var run = new AchievementBackfillEntity
        {
            Id = Guid.NewGuid(),
            StartedUtc = DateTime.UtcNow,
            StartedBy = request.StartedBy,
            UsersQueued = userIds.Count
        };
        dbContext.AchievementBackfills.Add(run);
        await dbContext.SaveChangesAsync(ct);

        foreach (var userId in userIds)
        {
            await queueService.SendMessageAsync(new CheckAchievementsMessage
            {
                UserId = userId,
                BackfillId = run.Id,
                QueuedTimeUtc = DateTime.UtcNow
            }, ct: ct);
        }

        logger.LogInformation("Achievement backfill {BackfillId} queued for {Count} users.", run.Id, userIds.Count);
        return AchievementBackfillDto.From(run);
    }

    public async Task<AchievementBackfillDto?> Handle(GetLatestAchievementBackfillQuery request, CancellationToken ct)
    {
        var run = await dbContext.AchievementBackfills.AsNoTracking().OrderByDescending(b => b.StartedUtc).FirstOrDefaultAsync(ct);
        return run == null ? null : AchievementBackfillDto.From(run);
    }
}
