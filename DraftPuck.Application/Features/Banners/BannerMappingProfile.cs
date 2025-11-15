namespace DraftPuck.Application.Features.Banners;

public class BannerMappingProfile : Profile
{
    public BannerMappingProfile()
    {
        CreateMap<BannerEntity, BannerDto>();
    }
}
