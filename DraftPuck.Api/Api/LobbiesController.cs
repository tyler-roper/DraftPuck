namespace DraftPuck.Api.Api
{
    public class LobbiesController : DraftPuckApiControllerBase
    {
        private readonly ILobbyService _lobbyService;
        private readonly IMapper _mapper;

        public LobbiesController(ILobbyService lobbyService, IMapper mapper)
        {
            _lobbyService = lobbyService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLobby(NewLobbyRequest request)
        {
            if (CurrentUser == null) return Unauthorized();
            if (string.IsNullOrEmpty(request.Name)) return BadRequest();

            var lobby = await _lobbyService.CreateLobby(CurrentUser.Id, request);

            return Created($"lobbies/{lobby.Id}", _mapper.Map<LobbyResponse>(lobby));
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetLobbyByCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest();

            var lobby = await _lobbyService.GetLobby(code);

            return lobby == null
                ? NotFound()
                : Ok(_mapper.Map<LobbyResponse>(lobby));
        }

        [HttpGet("{id}/events")]
        public async Task<IActionResult> GetLobbyEventsById(Guid id)
        {
            if (CurrentUser == null) return Unauthorized();

            try
            {
                var lobbyEvents = await _lobbyService.GetLobbyEvents(CurrentUser.Id, id);
                return Ok(lobbyEvents);
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("{code}/join")]
        public async Task<IActionResult> JoinLobbyByCode(string code, JoinLobbyRequest request)
        {
            if (CurrentUser == null && !request.IsBot) return Unauthorized();

            var userId = !request.IsBot
                ? CurrentUser!.Id
                : Guid.NewGuid();

            var lobby = await _lobbyService.JoinLobbyByCode(userId, code, request);
            if (lobby == null) return NotFound();

            return Ok(_mapper.Map<LobbyResponse>(lobby));
        }

        [HttpPost("{code}/pick")]
        public async Task<IActionResult> MakePick(string code, MakePickRequest request)
        {
            if (CurrentUser == null) return Unauthorized();

            try
            {
                var pick = await _lobbyService.MakePick(CurrentUser.Id, code, request);
                return Ok(_mapper.Map<LobbyMemberPickResponse>(pick));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{code}/drink/{drinkId}/assign")]
        public async Task<IActionResult> AssignDrink(string code, Guid drinkId, Guid recipientLobbyMemberId)
        {
            if (CurrentUser == null) return Unauthorized();

            try
            {
                var drink = await _lobbyService.AssignDrink(CurrentUser.Id, code, drinkId, recipientLobbyMemberId);
                return Ok(_mapper.Map<DrinkResponse>(drink));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{code}/changeName")]
        public async Task<IActionResult> ChangeName(string code, string newName)
        {
            if (CurrentUser == null) return Unauthorized();

            try
            {
                await _lobbyService.ChangeName(CurrentUser.Id, code, newName);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{code}/member/{lobbyMemberId}")]
        public async Task<IActionResult> RemoveLobbyMember(string code, Guid lobbyMemberId)
        {
            if (CurrentUser == null) return Unauthorized();

            try
            {
                await _lobbyService.RemoveLobbyMember(CurrentUser.Id, code, lobbyMemberId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{code}/pick/{pickId}")]
        public async Task<IActionResult> RemovePick(string code, Guid pickId)
        {
            if (CurrentUser == null) return Unauthorized();

            try
            {
                await _lobbyService.RemovePick(CurrentUser.Id, code, pickId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{code}/broadcast")]
        public async Task<IActionResult> Broadcast(string code, Broadcast broadcast)
        {
            try
            {
                await _lobbyService.Broadcast(code, broadcast.Message);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{code}/message")]
        public async Task<IActionResult> SendMessage(string code, MessageModel message)
        {
            try
            {
                await _lobbyService.SendMessage(CurrentUser!.Id, code, message.Message);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }

    public class Broadcast
    {
        public string Message { get; set; } = null!;
    }
}
