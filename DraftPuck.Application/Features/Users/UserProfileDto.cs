using DraftPuck.Application.Features.Achievements;
using DraftPuck.Application.Features.Banners;
using DraftPuck.Application.Features.Titles;

namespace DraftPuck.Application.Features.Users;

/// <summary>
/// Another player's public profile: what their profile page shows. Nothing private (no email, Discord ID or settings),
/// since any player can look anyone up by nickname.
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string? Nickname { get; set; }
    public bool IsGuest { get; set; }
    public bool IsBot { get; set; }
    public DateTime Created { get; set; }
    public string? AvatarPath { get; set; }
    public BannerDto? Banner { get; set; }
    public TitleDto? Title { get; set; }
    public List<UserAchievementDto> Achievements { get; set; } = [];
}
