namespace DraftPuck.Application.Features.Users;

public class GetUserByIdQuery : IRequest<UserDto>
{
    public Guid Id { get; set; }
}