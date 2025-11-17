namespace DraftPuck.Application.Features.Lobbies;

public class ChangeNameCommand : IRequest
{
    public string NewName { get; set; } = null!;
    public string Code { get; set; } = null!;
    public Guid UserId { get; set; }
}