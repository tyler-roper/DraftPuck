using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class GameTeamMappingProfile : Profile
{
    public GameTeamMappingProfile()
    {
        // Situations and Strength are set by the full game map (AfterMap); Roster is filled in during game processing.
        CreateMap<NhlTeamSummary, GameTeamDto>()
            .ForMember(dest => dest.Roster, opt => opt.Ignore())
            .ForMember(dest => dest.Situations, opt => opt.Ignore())
            .ForMember(dest => dest.Strength, opt => opt.Ignore());
    }
}