namespace DraftPuck.Application.Features.Lobbies.Recap;

/// <summary>A lobby's end-of-night awards. Like the lobby itself, anyone with the code can see them.</summary>
public class GetLobbyRecapQuery : IRequest<LobbyRecapDto>
{
    public string Code { get; set; } = null!;
}

public class LobbyRecapDto
{
    public List<LobbyAwardDto> Awards { get; set; } = [];
}

public class LobbyAwardDto
{
    /// <summary>Which award (<see cref="LobbyAwards"/>), for the client's icon.</summary>
    public string Key { get; set; } = null!;
    public string Title { get; set; } = null!;
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = null!;
    /// <summary>What they did, e.g. "McDavid (EDM) earned 4 drinks".</summary>
    public string Detail { get; set; } = null!;
}
