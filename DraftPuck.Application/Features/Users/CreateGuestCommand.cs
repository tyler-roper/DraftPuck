using DraftPuck.Application.Features.Auth;

namespace DraftPuck.Application.Features.Users;

public class CreateGuestCommand : IRequest<AuthenticationResultDto> {
    public string IpAddress { get; set; } = null!;
}