namespace DraftPuck.Application.Features.Games;

public class GetAllGameSummariesQueryHandler(IGameCache gameCache, IMapper mapper) : IRequestHandler<GetAllGameSummariesQuery, List<GameSummaryDto>>
{
    public async Task<List<GameSummaryDto>> Handle(GetAllGameSummariesQuery request, CancellationToken cancellationToken)
    {
        var games = await gameCache.GetAllGamesAsync();
        var summaries = mapper.Map<List<GameSummaryDto>>(games);
        return summaries;
    }
}