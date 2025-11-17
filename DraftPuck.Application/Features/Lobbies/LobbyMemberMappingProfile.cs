namespace DraftPuck.Application.Features.Lobbies;

public class LobbyMemberMappingProfile : Profile
{
    public LobbyMemberMappingProfile()
    {
        CreateMap<LobbyMemberEntity, LobbyMemberDto>()
            .ForMember(dest => dest.Picks, opt => opt.MapFrom(src => src.LobbyMemberPicks))
            .ForMember(
                dest => dest.Banner,
                opt => opt.MapFrom(src => src.User.UserBanners.Any()
                    ? src.User.UserBanners.FirstOrDefault(ub => ub.IsEquipped) != null
                        ? src.User.UserBanners.First(ub => ub.IsEquipped).Banner
                        : null
                    : null)
            )
            .ForMember(
                dest => dest.Title,
                opt => opt.MapFrom(src => src.User.UserTitles.Any()
                    ? src.User.UserTitles.FirstOrDefault(ub => ub.IsEquipped) != null
                        ? src.User.UserTitles.First(ub => ub.IsEquipped).Title
                        : null
                    : null)
            )
            .ForMember(dest => dest.IsGuest, opt => opt.MapFrom(src => src.User.IsGuest))
            .ForMember(dest => dest.AvatarPath, opt => opt.MapFrom(src => src.User.AvatarPath));
    }
}