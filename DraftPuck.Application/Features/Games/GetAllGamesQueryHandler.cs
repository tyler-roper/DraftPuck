namespace DraftPuck.Application.Features.Games;

public class GetAllGamesQueryHandler(IGameCache gameCache) : IRequestHandler<GetAllGamesQuery, List<GameDto>>
{
    public async Task<List<GameDto>> Handle(GetAllGamesQuery request, CancellationToken cancellationToken)
        => await gameCache.GetAllGamesAsync();
}