using DraftPuck.Infrastructure.Nhl.Models;
using System.Globalization;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public class ScheduleMappingProfile : Profile
{
    public ScheduleMappingProfile()
    {
        CreateMap<NhlSchedule, ScheduleDto>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => DateTime.ParseExact(src.GameWeek.First().Date, "yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .ForMember(dest => dest.Games, opt => opt.MapFrom(src => src.GameWeek.First().Games));
    }
}