namespace DraftPuck.Application.Features.Games;

/// <summary>One of today's games. For a client that missed a push, so can't apply the next one.</summary>
public record GetGameQuery(int GameId) : IRequest<GameDto>;
