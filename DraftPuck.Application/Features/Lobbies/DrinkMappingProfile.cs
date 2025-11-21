namespace DraftPuck.Application.Features.Lobbies;

public class DrinkMappingProfile : Profile
{
    public DrinkMappingProfile()
    {
        CreateMap<DrinkEntity, DrinkDto>();
    }
}