namespace DraftPuck.Application.Features.Users;

public class CreateUserCommand : IRequest<UserDto>
{
    public string Nickname { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public Guid GuestUserId { get; set; }
}