using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class PlayerSummaryMappingProfile : Profile
{
    public PlayerSummaryMappingProfile()
    {
        CreateMap<NhlPlayerSummary, PlayerSummaryDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Default))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Default))
            .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.SweaterNumber))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PlayerId));
    }
}