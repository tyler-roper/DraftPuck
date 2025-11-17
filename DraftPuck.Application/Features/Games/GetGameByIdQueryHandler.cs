using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Games;

public class GetGameByIdQueryHandler(IGameCache gameCache) : IRequestHandler<GetGameByIdQuery, GameDto>
{
    public async Task<GameDto> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
        => await gameCache.GetGameByIdAsync(request.GameId) ?? throw new NotFoundException($"Game not found with ID {request.GameId}");
}