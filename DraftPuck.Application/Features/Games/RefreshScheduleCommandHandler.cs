using Microsoft.Extensions.Options;

namespace DraftPuck.Application.Features.Games;
public class RefreshScheduleCommandHandler(INhlQueueService nhlQueue, IGameCache gameCache, INhlClient nhlClient, IOptions<ApplicationOptions> appConfig) : IRequestHandler<RefreshScheduleCommand>
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public async Task Handle(RefreshScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await nhlClient.GetScheduleAsync(GameProcessingHelpers.AdjustDateTimeForCutoff(_appConfig.CurrentTimeUtc));
        var games = schedule.Games;
        var currentIds = schedule.Games.Select(g => g.Id).ToHashSet();

        var cachedGames = await gameCache.GetAllGamesAsync();

        foreach (var g in cachedGames.Where(cg => !currentIds.Contains(cg.Id)))
            await gameCache.RemoveGameAsync(g);

        var tasks = schedule.Games.Select(game =>
            nhlQueue.SendMessageAsync(new ProcessGameMessage(game.Id, true))
        );

        await Task.WhenAll(tasks);
    }
}
