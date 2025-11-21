using DraftPuck.Application.Features.Achievements;

namespace DraftPuck.Web.Features.Achievements;

public class AchievementsController(IMediator mediator) : BaseController()
{
    [HttpGet]
    public async Task<ActionResult<List<AchievementDto>>> GetAllAchievements()
    {
        var query = new GetAllAchievementsQuery();
        var achievementDtos = await mediator.Send(query);
        return Ok(achievementDtos);
    }
}
