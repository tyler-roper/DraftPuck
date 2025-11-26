namespace DraftPuck.Application.Features.Lobbies.Events;

public class LobbyEventMappingProfile : Profile
{
    public LobbyEventMappingProfile()
    {
        CreateMap<LobbyEventEntity, LobbyEventDto>();
    }
}
