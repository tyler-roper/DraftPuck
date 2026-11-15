namespace DraftPuck.Application.Features.Lobbies.Picks;

public enum BotFallbackPickStrategy
{
    BestAvailable,
    BestBelowRange,
    WorstAboveRange,
    WorstAvailable,
    Random
}
