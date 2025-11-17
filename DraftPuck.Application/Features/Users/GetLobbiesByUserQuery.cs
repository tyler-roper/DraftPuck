using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Features.Users;

public class GetLobbiesByUserQuery : IRequest<List<UserLobbySummaryDto>>
{
    public Guid UserId { get; set; }
}