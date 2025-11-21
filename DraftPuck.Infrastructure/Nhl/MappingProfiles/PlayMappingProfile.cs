using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class PlayMappingProfile : Profile
{
    public PlayMappingProfile()
    {
        CreateMap<NhlPlay, PlayDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.EventId))
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => src.PeriodDescriptor.Number))
            .ForMember(dest => dest.PeriodType, opt => opt.MapFrom(src => NhlMappingHelpers.MapPeriodType(src.PeriodDescriptor.PeriodType)))
            .ForMember(dest => dest.TimeRemainingInPeriod, opt => opt.MapFrom(src => src.TimeRemaining))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => NhlMappingHelpers.MapPlayType(src.TypeDescKey)))
            .ForMember(dest => dest.PrimaryPlayerId, opt => opt.MapFrom(src => NhlMappingHelpers.MapPrimaryPlayerId(src)))
            .ForMember(dest => dest.PrimaryTeamId, opt => opt.MapFrom(src => src.Details.EventOwnerTeamId))
            .ForMember(dest => dest.HomeScore, opt => opt.MapFrom(src => src.Details.HomeScore))
            .ForMember(dest => dest.AwayScore, opt => opt.MapFrom(src => src.Details.AwayScore))
            .ForMember(dest => dest.Penalty, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Details.DescKey) ? null : NhlMappingHelpers.KebabToCamelCase(src.Details.DescKey)));
    }
}