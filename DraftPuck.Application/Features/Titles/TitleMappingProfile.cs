using DraftPuck.Shared.Titles;

namespace DraftPuck.Application.Features.Titles;

public class TitleMappingProfile : Profile
{
    public TitleMappingProfile()
    {
        CreateMap<TitleEntity, TitleDto>();
    }
}
