namespace DraftPuck.Application.Features.Users;

public class GetUserByNameQuery : IRequest<UserDto>
{
    public string Name { get; set; } = null!;
}