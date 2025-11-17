using DraftPuck.Application.Features.Achievements;

namespace DraftPuck.Application.Features.Users;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<UserEntity, UserDto>()
            .ForMember(
                dest => dest.Banner,
                opt => opt.MapFrom(src => src.UserBanners.Any()
                    ? src.UserBanners.Single(ub => ub.IsEquipped).Banner
                    : null)
            )
            .ForMember(
                dest => dest.Title,
                opt => opt.MapFrom(src => src.UserTitles.Any()
                    ? src.UserTitles.Single(ub => ub.IsEquipped).Title
                    : null)
            )
            .ForMember(
                dest => dest.OwnedBanners,
                opt => opt.MapFrom(src => src.UserBanners)
            )
            .ForMember(
                dest => dest.OwnedTitles,
                opt => opt.MapFrom(src => src.UserTitles)
            )
            .ForMember(
                dest => dest.Achievements,
                opt => opt.MapFrom(src => src.UserAchievements)
            );

        CreateMap<UserAchievementEntity, UserAchievementDto>()
            .ForMember(
                dest => dest.UniqueIdentifier,
                opt => opt.MapFrom(src => src.Achievement.UniqueIdentifier)
            )
            .ForMember(
                dest => dest.FriendlyName,
                opt => opt.MapFrom(src => src.Achievement.FriendlyName)
            )
            .ForMember(
                dest => dest.Description,
                opt => opt.MapFrom(src => src.Achievement.Description)
            );
    }
}