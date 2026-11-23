namespace DraftPuck.Web.Features.Lobbies;

public class MergeLobbyMembersRequestDto
{
    /// <summary>The spot to remove, whose login takes over the original spot in the route.</summary>
    public Guid DuplicateMemberId { get; set; }
}
