namespace DraftPuck.Application.Features.Lobbies.Members;

/// <summary>
/// The host says two members are the same person: a guest who lost their session (a closed incognito tab, another
/// device) and joined again under a new name. The person keeps the original spot and plays it from their new login;
/// the duplicate spot is removed, with anything it did.
/// </summary>
public class MergeLobbyMembersCommand : IRequest
{
    public string Code { get; set; } = null!;
    /// <summary>The spot that stays: the guest's first one, with their name, picks and drinks.</summary>
    public Guid OriginalMemberId { get; set; }
    /// <summary>The spot that goes: the one the person joined again with. Its login takes over the original spot.</summary>
    public Guid DuplicateMemberId { get; set; }
    public Guid RequesterUserId { get; set; }
}
