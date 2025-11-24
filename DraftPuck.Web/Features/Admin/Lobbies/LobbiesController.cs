using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Web.Features.Admin.Lobbies;

public class LobbiesController(IMediator mediator) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAllActiveLobbies(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? userId,
        [FromQuery] bool activeOnly = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool includeRemovedUsers = false)
    {
        var query = new GetAllLobbiesQuery()
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            ActiveOnly = activeOnly,
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            IncludeRemovedUsers = includeRemovedUsers
        };

        var lobbyDtos = await mediator.Send(query);
        return Ok(lobbyDtos);
    }
}