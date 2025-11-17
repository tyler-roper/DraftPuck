namespace DraftPuck.Application.Features.Games;

public class GetGameByIdQuery : IRequest<GameDto>
{
    public int GameId { get; set; }
}