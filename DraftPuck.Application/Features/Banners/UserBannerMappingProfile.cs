namespace DraftPuck.Application.Features.Banners;

public class UserBannerMappingProfile : Profile
{
    public UserBannerMappingProfile()
    {
        CreateMap<UserBannerEntity, BannerDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.BannerId))
            .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.Banner.ImagePath))
            .ForMember(dest => dest.UniqueIdentifier, opt => opt.MapFrom(src => src.Banner.UniqueIdentifier));
    }
}