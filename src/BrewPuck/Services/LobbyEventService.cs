namespace BrewPuck.Services
{
    public class LobbyEventService : ILobbyEventService
    {
        private readonly IEventService _eventService;

        public LobbyEventService(IEventService eventService)
        {
            _eventService = eventService;
        }

        public void SendMessage(LobbyEventModel lobbyEvent)
        {
            _eventService.Notify(lobbyEvent);
        }
    }
}
