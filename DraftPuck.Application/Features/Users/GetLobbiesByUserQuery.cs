using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Users;

/// <summary>The lobbies a user is in (not removed from): all of them, or just tonight's live ones.</summary>
public class GetLobbiesByUserQuery : IRequest<List<LobbySummaryDto>>
{
    public Guid UserId { get; set; }
    /// <summary>Only lobbies still going (tonight's), e.g. to offer a quick rejoin.</summary>
    public bool ActiveOnly { get; set; }
}
