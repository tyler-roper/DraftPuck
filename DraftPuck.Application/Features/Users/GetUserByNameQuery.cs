namespace DraftPuck.Application.Features.Users;

/// <summary>A player's public profile, by nickname.</summary>
public class GetUserByNameQuery : IRequest<UserProfileDto>
{
    public string Name { get; set; } = null!;
}