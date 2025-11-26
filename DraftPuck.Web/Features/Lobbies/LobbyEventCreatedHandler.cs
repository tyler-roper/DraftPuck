using AutoMapper;
using DraftPuck.Application.Features.Lobbies.Events;
using DraftPuck.Application.Features.Lobbies.Messages;

namespace DraftPuck.Web.Features.Lobbies;

public class LobbyEventCreatedHandler(IClientEventService clientEventService, IMapper mapper) :
    INotificationHandler<LobbyEventCreatedNotification>,
    INotificationHandler<GlobalEventCreatedNotification>,
    INotificationHandler<MessageSentNotification>,
    INotificationHandler<LobbyStateChangedNotification>
{
    public async Task Handle(LobbyEventCreatedNotification notification, CancellationToken ct)
    {
        var lobbyEventDto = mapper.Map<LobbyEventDto>(notification.Event);
        await clientEventService.SendLobbyEvent(notification.JoinCode, lobbyEventDto, ct);
    }

    public async Task Handle(GlobalEventCreatedNotification notification, CancellationToken ct)
    {
        var lobbyEventDto = mapper.Map<LobbyEventDto>(notification.Event);
        await clientEventService.SendGlobalLobbyEvent(lobbyEventDto, ct);
    }

    public async Task Handle(MessageSentNotification notification, CancellationToken ct)
    {
        var messageDto = mapper.Map<MessageDto>(notification.Data.Message);
        await clientEventService.SendMessage(notification.Data.Lobby.JoinCode, messageDto, ct);
    }

    public async Task Handle(LobbyStateChangedNotification notification, CancellationToken ct)
    {
        await clientEventService.SendLobbyStateChangedNotification(notification.Data.LobbyJoinCode, ct);
    }
}
