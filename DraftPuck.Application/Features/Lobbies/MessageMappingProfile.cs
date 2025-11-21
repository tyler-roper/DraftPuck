namespace DraftPuck.Application.Features.Lobbies;

public class MessageMappingProfile : Profile
{
    public MessageMappingProfile()
    {
        CreateMap<MessageEntity, MessageDto>();
    }
}