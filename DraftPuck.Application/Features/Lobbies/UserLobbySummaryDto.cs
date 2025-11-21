namespace DraftPuck.Application.Features.Lobbies;

public class UserLobbySummaryDto
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public string JoinCode { get; set; } = null!;
    public DateTime Created { get; set; }
    public Guid CreatedBy { get; set; }
    public int PicksPerTeam { get; set; }
    public bool IsBotAutoPickingEnabled { get; set; }
    public int GameCount { get; set; }
    public int MemberCount { get; set; }
    public int DrinksGiven { get; set; }
    public int DrinksTaken { get; set; }
    public int DrinksPending { get; set; }
}