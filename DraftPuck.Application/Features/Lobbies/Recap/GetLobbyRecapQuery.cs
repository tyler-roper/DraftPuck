namespace DraftPuck.Application.Features.Lobbies.Recap;

/// <summary>A lobby's end-of-night awards. Like the lobby itself, anyone with the code can see them.</summary>
public class GetLobbyRecapQuery : IRequest<LobbyRecapDto>
{
    public string Code { get; set; } = null!;
}

public class LobbyRecapDto
{
    public List<LobbyAwardDto> Awards { get; set; } = [];
    /// <summary>The players picked in the lobby, named: the post-game page doesn't load old games itself.</summary>
    public List<RecapPlayerDto> Players { get; set; } = [];
}

public class RecapPlayerDto
{
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    /// <summary>"Cale Makar".</summary>
    public string Name { get; set; } = null!;
    /// <summary>The team's abbreviation, e.g. "COL".</summary>
    public string Team { get; set; } = null!;
}

public class LobbyAwardDto
{
    /// <summary>Which award (<see cref="LobbyAwards"/>), for the client's icon.</summary>
    public string Key { get; set; } = null!;
    public string Title { get; set; } = null!;
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = null!;
    /// <summary>For a rivalry, the other member (the client shows both).</summary>
    public Guid? OtherMemberId { get; set; }
    /// <summary>What they did, e.g. "McDavid (EDM) earned 4 drinks".</summary>
    public string Detail { get; set; } = null!;
}
