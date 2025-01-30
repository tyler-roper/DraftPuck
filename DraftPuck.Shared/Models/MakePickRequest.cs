namespace DraftPuck.Shared.Models;

public class MakePickRequest
{
    public Guid? LobbyMemberId { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
}