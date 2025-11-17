namespace DraftPuck.Application.Features.Games;
public record ProcessGameCommand(int GameId, bool IsInitialPopulation) : IRequest;
