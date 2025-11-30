using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Common.QueryExtensions;

namespace DraftPuck.Application.Features.Lobbies.Management;

public class GetLobbyByCodeQueryHandler(IDbContext dbContext, IMapper mapper) : IRequestHandler<GetLobbyByCodeQuery, LobbyDto>
{
    public async Task<LobbyDto> Handle(GetLobbyByCodeQuery request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Code))
        {
            throw new BadRequestException("Lobby code is required.");
        }

        var lobby = await dbContext.Lobbies
            .AsNoTracking()
            .IncludeLobbyDetails()
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, ct);

        if (lobby == null) throw new NotFoundException("Lobby not found.");
        //if (lobby.IsActive == false) throw new BadRequestException("Lobby is no longer active.");
        return mapper.Map<LobbyDto>(lobby);
    }
}