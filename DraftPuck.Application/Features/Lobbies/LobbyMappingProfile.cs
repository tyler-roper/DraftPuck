namespace DraftPuck.Application.Features.Lobbies;

public class LobbyMappingProfile : Profile
{
    public LobbyMappingProfile()
    {
        CreateMap<LobbyEntity, LobbyDto>()
            .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.LobbyMembers));
    }
}