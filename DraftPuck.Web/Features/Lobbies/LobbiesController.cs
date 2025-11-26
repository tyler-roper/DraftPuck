using DraftPuck.Application.Features.Lobbies;
using DraftPuck.Application.Features.Lobbies.Drinks;
using DraftPuck.Application.Features.Lobbies.Events;
using DraftPuck.Application.Features.Lobbies.Management;
using DraftPuck.Application.Features.Lobbies.Members;
using DraftPuck.Application.Features.Lobbies.Messages;
using DraftPuck.Application.Features.Lobbies.Picks;
using DraftPuck.Application.Features.Users;

namespace DraftPuck.Web.Features.Lobbies;

public class LobbiesController(IMediator mediator) : BaseController()
{
    [HttpPost]
    public async Task<IActionResult> CreateLobby(CreateLobbyRequestDto request)
    {
        var command = new CreateLobbyCommand()
        {
            CreatorUserId = CurrentUserId,
            IsBotAutoPickingEnabled = request.IsBotAutoPickingEnabled,
            Bots = request.Bots,
            GameIds = request.GameIds,
            Name = request.Name,
            PicksPerTeam = request.PicksPerTeam
        };

        var lobbyDto = await mediator.Send(command);
        return Created($"/api/lobbies/{lobbyDto.Id}", lobbyDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetLobbiesForCurrentUser()
    {
        var query = new GetLobbiesByUserQuery() { UserId = CurrentUserId };
        var lobbyDtos = await mediator.Send(query);
        return Ok(lobbyDtos);
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<LobbyDto>> GetLobbyByCode(string code)
    {
        var query = new GetLobbyByCodeQuery { Code = code };
        var lobbyDto = await mediator.Send(query);
        return Ok(lobbyDto);
    }

    [HttpGet("{id}/events")]
    public async Task<IActionResult> GetLobbyEventsById(Guid id)
    {
        var query = new GetLobbyEventsQuery { LobbyId = id, UserId = CurrentUserId };
        var events = await mediator.Send(query);
        return Ok(events);
    }

    [HttpPost("{code}/join")]
    public async Task<ActionResult<LobbyDto>> JoinLobbyByCode(string code, JoinLobbyRequestDto request)
    {
        var command = new JoinLobbyCommand()
        {
            Name = request.Name,
            IsBot = request.IsBot,
            BotPickStyle = request.BotPickStyle,
            Code = code,
            UserId = CurrentUserId
        };

        var lobbyDto = await mediator.Send(command);
        return Ok(lobbyDto);
    }

    [HttpPost("{code}/pick")]
    public async Task<ActionResult<LobbyMemberPickDto>> MakePick(string code, MakePickRequestDto request)
    {
        var command = new MakePickCommand()
        {
            GameId = request.GameId,
            LobbyMemberId = request.LobbyMemberId,
            PlayerId = request.PlayerId,
            TeamId = request.TeamId,
            Code = code,
            UserId = CurrentUserId
        };

        var pickDto = await mediator.Send(command);
        return Ok(pickDto);
    }

    [HttpPost("{code}/drink/{drinkId}/assign")]
    public async Task<ActionResult<DrinkDto>> AssignDrink(string code, Guid drinkId, AssignDrinkRequestDto request)
    {
        var command = new AssignDrinkCommand
        {
            Code = code,
            DrinkId = drinkId,
            RecipientLobbyMemberId = request.RecipientLobbyMemberId,
            AssignerUserId = CurrentUserId
        };
        var drinkDto = await mediator.Send(command);
        return Ok(drinkDto);
    }

    [HttpPost("{code}/changeName")]
    public async Task<IActionResult> ChangeName(string code, ChangeLobbyNameRequestDto request)
    {
        var command = new ChangeNameCommand
        {
            Code = code,
            NewName = request.NewName,
            UserId = CurrentUserId
        };
        await mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{code}/member/{lobbyMemberId}")]
    public async Task<IActionResult> RemoveLobbyMember(string code, Guid lobbyMemberId)
    {
        var command = new RemoveLobbyMemberCommand
        {
            Code = code,
            LobbyMemberId = lobbyMemberId,
            RequesterUserId = CurrentUserId
        };
        await mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{code}/pick/{pickId}")]
    public async Task<IActionResult> RemovePick(string code, Guid pickId)
    {
        var command = new RemovePickCommand
        {
            Code = code,
            PickId = pickId,
            UserId = CurrentUserId
        };
        await mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{code}/message")]
    public async Task<IActionResult> SendMessage(string code, SendLobbyMessageRequestDto request)
    {
        var command = new SendLobbyMessageCommand()
        {
            Message = request.Message,
            Code = code,
            UserId = CurrentUserId
        };

        await mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{code}/member/me")]
    public async Task<IActionResult> LeaveLobby(string code)
    {
        var command = new LeaveLobbyCommand
        {
            Code = code,
            UserId = CurrentUserId
        };

        await mediator.Send(command);
        return NoContent();
    }
}