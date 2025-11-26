using DraftPuck.Application.Features.Admin.Lobbies;

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
        if (dateFrom == null)
            dateFrom = DateTime.UtcNow.AddDays(-1);

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