using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class PeriodSummaryMappingProfile : Profile
{
    public PeriodSummaryMappingProfile()
    {
        CreateMap<NhlLinescorePeriod, PeriodSummaryDto>()
            .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.PeriodDescriptor.Number))
            .ForMember(dest => dest.HomeGoals, opt => opt.MapFrom(src => src.Home))
            .ForMember(dest => dest.AwayGoals, opt => opt.MapFrom(src => src.Away))
            .ForMember(dest => dest.PeriodType, opt => opt.MapFrom(src => NhlMappingHelpers.MapPeriodType(src.PeriodDescriptor.PeriodType)));
    }
}