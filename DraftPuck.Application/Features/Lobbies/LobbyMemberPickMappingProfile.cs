namespace DraftPuck.Application.Features.Lobbies;

public class LobbyMemberPickMappingProfile : Profile
{
    public LobbyMemberPickMappingProfile()
    {
        CreateMap<LobbyMemberPickEntity, LobbyMemberPickDto>();
    }
}