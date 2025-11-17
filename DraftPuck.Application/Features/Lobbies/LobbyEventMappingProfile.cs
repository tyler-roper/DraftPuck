namespace DraftPuck.Application.Features.Lobbies;

public class LobbyEventMappingProfile : Profile
{
    public LobbyEventMappingProfile()
    {
        CreateMap<LobbyEventEntity, LobbyEventDto>();
    }
}
