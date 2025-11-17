namespace DraftPuck.Application.Features.Titles;

public class UserTitleMappingProfile : Profile
{
    public UserTitleMappingProfile()
    {
        CreateMap<UserTitleEntity, TitleDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.TitleId))
            .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Title.Text))
            .ForMember(dest => dest.UniqueIdentifier, opt => opt.MapFrom(src => src.Title.UniqueIdentifier));
    }
}