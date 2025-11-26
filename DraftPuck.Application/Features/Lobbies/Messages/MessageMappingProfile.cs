namespace DraftPuck.Application.Features.Lobbies.Messages;

public class MessageMappingProfile : Profile
{
    public MessageMappingProfile()
    {
        CreateMap<MessageEntity, MessageDto>();
    }
}