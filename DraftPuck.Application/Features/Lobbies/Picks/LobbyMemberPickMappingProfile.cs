namespace DraftPuck.Application.Features.Lobbies.Picks;

public class LobbyMemberPickMappingProfile : Profile
{
    public LobbyMemberPickMappingProfile()
    {
        CreateMap<LobbyMemberPickEntity, LobbyMemberPickDto>();
    }
}