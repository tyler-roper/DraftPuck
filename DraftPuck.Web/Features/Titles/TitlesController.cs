using DraftPuck.Application.Features.Titles;

namespace DraftPuck.Web.Features.Titles;

public class TitlesController(IMediator mediator) : BaseController()
{
    [HttpGet]
    public async Task<ActionResult<List<TitleDto>>> GetAllTitles()
    {
        var query = new GetAllTitlesQuery();
        var titleDtos = await mediator.Send(query);
        return Ok(titleDtos);
    }
}
