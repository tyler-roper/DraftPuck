namespace DraftPuck.Core.Models;

public class LobbyResponse
{
    public Guid Id { get; set; }
    public string JoinCode { get; set; } = null!;
    public LobbyStatus Status { get; set; }
    public int PicksPerTeam { get; set; }
    public DateTime Created { get; set; }
    public Guid CreatedBy { get; set; }
    public List<LobbyMemberResponse> Members { get; set; } = new();
}