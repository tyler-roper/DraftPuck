using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Users;

public class GetLobbiesByUserQuery : IRequest<List<LobbySummaryDto>>
{
    public Guid UserId { get; set; }
}