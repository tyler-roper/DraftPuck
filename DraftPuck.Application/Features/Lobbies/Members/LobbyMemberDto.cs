using DraftPuck.Application.Features.Banners;
using DraftPuck.Application.Features.Lobbies.Drinks;
using DraftPuck.Application.Features.Lobbies.Messages;
using DraftPuck.Application.Features.Lobbies.Picks;
using DraftPuck.Application.Features.Titles;

namespace DraftPuck.Application.Features.Lobbies.Members;

public class LobbyMemberDto
{
    public Guid Id { get; set; }
    public Guid LobbyId { get; set; }
    public Guid UserId { get; set; }
    public DateTime Joined { get; set; }
    public string Name { get; set; } = null!;
    public bool IsBot { get; set; } = false;
    public bool IsRemoved { get; set; } = false;
    public BotPickStyle? BotPickStyle { get; set; }

    public List<DrinkDto> Drinks { get; set; } = [];
    public List<LobbyMemberPickDto> Picks { get; set; } = [];
    public List<MessageDto> Messages { get; set; } = [];

    public bool IsGuest { get; set; } = true;
    public BannerDto? Banner { get; set; }
    public TitleDto? Title { get; set; }
    public string? AvatarPath { get; set; }
}