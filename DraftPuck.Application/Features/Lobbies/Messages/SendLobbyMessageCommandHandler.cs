using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Lobbies.Messages;

public class SendLobbyMessageCommandHandler(IDbContext dbContext, IMediator mediator) : IRequestHandler<SendLobbyMessageCommand>
{
    public const int MaxMessageLength = 400;

    public async Task Handle(SendLobbyMessageCommand request, CancellationToken ct)
    {
        // The chat box enforces these too, but anyone can call the API directly, and a message is stored, sent to the
        // whole lobby and pushed to phones.
        var text = request.Message?.Trim() ?? "";
        if (text.Length == 0)
            throw new BadRequestException("Message is empty.");
        if (text.Length > MaxMessageLength)
            throw new BadRequestException($"Message exceeds {MaxMessageLength} characters.");

        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, ct) ?? throw new NotFoundException("Lobby not found.");

        var sender = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == request.UserId)
            ?? throw new NotFoundException("User not found in lobby.");

        var message = new MessageEntity
        {
            LobbyMemberId = sender.Id,
            Message = text
        };

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(ct);

        await mediator.Publish(new MessageSentNotification(new(lobby, sender, message)), ct);
    }
}