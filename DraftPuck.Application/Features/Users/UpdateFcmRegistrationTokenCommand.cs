namespace DraftPuck.Application.Features.Users;

public class UpdateFcmRegistrationTokenCommand : IRequest<UserDto>
{
    public string? Token { get; set; }
    public Guid UserId { get; set; }
}
