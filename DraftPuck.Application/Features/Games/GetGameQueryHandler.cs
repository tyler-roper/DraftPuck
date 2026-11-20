using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Games;

public class GetGameQueryHandler(IGameCache gameCache) : IRequestHandler<GetGameQuery, GameDto>
{
    public async Task<GameDto> Handle(GetGameQuery request, CancellationToken cancellationToken)
        => await gameCache.GetGameByIdAsync(request.GameId) ?? throw new NotFoundException("Game not found.");
}
