using DraftPuck.Application.Features.Users;

namespace DraftPuck.Application.Features.Admin.Users;

public class GetAllUsersQuery : IRequest<IEnumerable<UserDto>>
{
    public string? Nickname { get; set; }
    public bool ActiveOnly { get; set; }
    public bool IncludeGuests { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}