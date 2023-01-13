using DraftPuck.Api;

namespace DraftPuck.Models;

public class LobbyProfile : Profile
{
    public LobbyProfile()
    {
        CreateMap<Lobby, LobbyResponse>()
            .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.LobbyMembers));
    }
}

public class LobbyMemberProfile : Profile
{
    public LobbyMemberProfile()
    {
        CreateMap<LobbyMember, LobbyMemberResponse>()
            .ForMember(dest => dest.Picks, opt => opt.MapFrom(src => src.LobbyMemberPicks));
    }
}

public class LobbyMemberPickProfile : Profile
{
    public LobbyMemberPickProfile()
    {
        CreateMap<LobbyMemberPick, LobbyMemberPickResponse>();
    }
}

public class DrinkProfile : Profile
{
    public DrinkProfile()
    {
        CreateMap<Drink, DrinkResponse>();
    }
}

