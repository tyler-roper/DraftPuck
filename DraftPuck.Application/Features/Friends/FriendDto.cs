using DraftPuck.Application.Features.Banners;
using DraftPuck.Application.Features.Titles;

namespace DraftPuck.Application.Features.Friends;

/// <summary>Another player, as a friends list shows them: enough to draw them, nothing private.</summary>
public class FriendDto
{
    public Guid Id { get; set; }
    public string Nickname { get; set; } = null!;
    public string? AvatarPath { get; set; }
    public BannerDto? Banner { get; set; }
    public TitleDto? Title { get; set; }
}

/// <summary>A user's friends, and the requests waiting on them or on the other person.</summary>
public class FriendsDto
{
    public List<FriendDto> Friends { get; set; } = [];
    /// <summary>Requests others sent this user, to accept or decline.</summary>
    public List<FriendDto> Incoming { get; set; } = [];
    /// <summary>Requests this user sent that haven't been answered.</summary>
    public List<FriendDto> Outgoing { get; set; } = [];
    /// <summary>Invites to lobbies that are still on, that this user hasn't joined or dismissed. Newest first.</summary>
    public List<LobbyInviteDto> Invites { get; set; } = [];
    /// <summary>
    /// Players with an account this user has been in a lobby with, who aren't friends yet and haven't been asked either
    /// way. Most recently played with first, at most <see cref="GetFriendsQueryHandler.MaxSuggestions"/>.
    /// </summary>
    public List<FriendDto> Suggestions { get; set; } = [];
}

/// <summary>An invite to a lobby, as the person invited sees it.</summary>
public class LobbyInviteDto
{
    public string JoinCode { get; set; } = null!;
    public FriendDto Sender { get; set; } = null!;
    public int GameCount { get; set; }
    public int MemberCount { get; set; }
    public DateTime Created { get; set; }
}
