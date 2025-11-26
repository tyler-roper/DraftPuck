namespace DraftPuck.Application.Features.Lobbies.Drinks;

public class DrinkMappingProfile : Profile
{
    public DrinkMappingProfile()
    {
        CreateMap<DrinkEntity, DrinkDto>();
    }
}