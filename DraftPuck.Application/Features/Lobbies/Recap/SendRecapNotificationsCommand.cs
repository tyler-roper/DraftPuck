namespace DraftPuck.Application.Features.Lobbies.Recap;

/// <summary>
/// The day after a night of lobbies, at noon Eastern: one push per person who was in a lobby that saw drinks, naming
/// what they won and earned, and opening that lobby's post-game page.
/// </summary>
public class SendRecapNotificationsCommand : IRequest;
