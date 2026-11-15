using DraftPuck.Application.Features.Admin.Achievements;
using System.Security.Claims;

namespace DraftPuck.Web.Features.Admin.Achievements;

public class AchievementsController(IMediator mediator) : AdminBaseController
{
    /// <summary>Starts a backfill: a silent check of every achievement for everyone who has played. Returns the new run.</summary>
    [HttpPost("backfill")]
    public async Task<ActionResult<AchievementBackfillDto>> Backfill()
    {
        var adminId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
        return Ok(await mediator.Send(new BackfillAchievementsCommand { StartedBy = adminId }));
    }

    /// <summary>The latest backfill run and its progress; 204 if there's never been one.</summary>
    [HttpGet("backfill/latest")]
    public async Task<ActionResult<AchievementBackfillDto>> LatestBackfill()
    {
        var run = await mediator.Send(new GetLatestAchievementBackfillQuery());
        return run == null ? NoContent() : Ok(run);
    }
}
