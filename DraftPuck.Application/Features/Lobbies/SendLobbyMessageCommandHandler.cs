using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Lobbies;

public class SendLobbyMessageCommandHandler(IDbContext dbContext, IMediator mediator) : IRequestHandler<SendLobbyMessageCommand>
{
    public async Task Handle(SendLobbyMessageCommand request, CancellationToken ct)
    {
        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, ct) ?? throw new NotFoundException("Lobby not found.");

        var sender = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == request.UserId)
            ?? throw new NotFoundException("User not found in lobby.");

        var message = new MessageEntity
        {
            LobbyMemberId = sender.Id,
            Message = request.Message
        };

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(ct);

        await mediator.Publish(new MessageSentNotification(new(lobby, sender, message)), ct);
    }
}