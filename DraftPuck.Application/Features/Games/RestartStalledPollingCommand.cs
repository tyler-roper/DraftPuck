namespace DraftPuck.Application.Features.Games;

/// <summary>Restarts polling of today's unfinished games whose chain of poll messages has stopped.</summary>
public record RestartStalledPollingCommand : IRequest;
