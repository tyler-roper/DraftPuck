using DraftPuck.Application.Features.Admin.Users;

namespace DraftPuck.Web.Features.Admin.Users;

public class UsersController(IMediator mediator) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] string? nickname,
        [FromQuery] bool includeGuests = false,
        [FromQuery] bool activeOnly = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = new GetAllUsersQuery()
        {
            Nickname = nickname,
            IncludeGuests = includeGuests,
            ActiveOnly = activeOnly,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var userDtos = await mediator.Send(query);
        return Ok(userDtos);
    }
}