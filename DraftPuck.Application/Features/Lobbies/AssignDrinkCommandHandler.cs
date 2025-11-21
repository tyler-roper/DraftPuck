using DraftPuck.Application.Common.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Lobbies;

public class AssignDrinkCommandHandler(IDbContext dbContext, IMapper mapper, IMediator mediator) : IRequestHandler<AssignDrinkCommand, DrinkDto>
{
    public async Task<DrinkDto> Handle(AssignDrinkCommand request, CancellationToken ct)
    {
        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
                .ThenInclude(lm => lm.LobbyMemberPicks.Where(lmp => lmp.IsActive))
                    .ThenInclude(lmp => lmp.Drinks)
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, ct);

        if (lobby == null)
        {
            throw new NotFoundException("Lobby not found.");
        }

        var sender = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == request.AssignerUserId)
            ?? throw new NotFoundException("Sender not found in lobby.");

        var recipient = lobby.LobbyMembers.FirstOrDefault(m => m.Id == request.RecipientLobbyMemberId)
            ?? throw new NotFoundException("Recipient not found in lobby.");

        var drink = lobby.LobbyMembers.SelectMany(m => m.LobbyMemberPicks)
            .SelectMany(p => p.Drinks)
            .FirstOrDefault(d => d.Id == request.DrinkId)
            ?? throw new NotFoundException("Drink not found in lobby.");

        if (drink.RecipientLobbyMemberId != null)
        {
            throw new ValidationException("Drink has already been assigned.");
        }

        drink.Assigned = DateTime.UtcNow;
        drink.RecipientLobbyMemberId = recipient.Id;

        await dbContext.SaveChangesAsync(ct);

        await mediator.Publish(new DrinkAssignedNotification(new(lobby, sender, recipient, drink)), ct);

        return mapper.Map<DrinkDto>(drink);
    }
}