using DraftPuck.Application.Common.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Lobbies;

public class ChangeLobbyNameCommandHandler(IDbContext dbContext, IMediator mediator) : IRequestHandler<ChangeNameCommand>
{
    public async Task Handle(ChangeNameCommand request, CancellationToken cancellationToken)
    {
        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers)
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, cancellationToken)
            ?? throw new NotFoundException($"Lobby with code '{request.Code}' not found.");

        var lobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == request.UserId && !lm.IsRemoved) ?? throw new NotFoundException($"User not found or is removed from lobby '{request.Code}'.");

        var newName = request.NewName.Trim();
        if (string.IsNullOrEmpty(newName))
            throw new ValidationException("New name cannot be empty.");

        if (lobbyMember.Name == newName)
            return;

        if (lobby.LobbyMembers.Any(lm => lm.Id != lobbyMember.Id && !lm.IsRemoved && lm.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException($"The name '{newName}' is already taken in this lobby.");

        var oldName = lobbyMember.Name;
        lobbyMember.Name = newName;

        await dbContext.SaveChangesAsync(cancellationToken);

        var payload = new LobbyNameChangeEventPayload(lobby, lobbyMember, oldName);
        await mediator.Publish(new UserNameChangedNotification(payload), cancellationToken);
    }
}