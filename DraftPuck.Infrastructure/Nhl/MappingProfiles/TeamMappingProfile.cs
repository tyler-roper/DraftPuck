using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class TeamMappingProfile : Profile
{
    public TeamMappingProfile()
    {
        CreateMap<NhlTeamSummary, TeamDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.CommonName.Default))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => NhlMappingHelpers.MapLocation(src.PlaceName)))
            .ForMember(dest => dest.Abbreviation, opt => opt.MapFrom(src => src.Abbrev))
            .Include<NhlTeamSummary, GameTeamDto>();
    }
}