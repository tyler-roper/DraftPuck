using Draftpuck.Nhl.Services.Interfaces;
using System.Globalization;

namespace DraftPuck.Api.Api
{
    public class NhlController : DraftPuckApiControllerBase
    {
        private readonly INhlService _nhlService;
        private readonly IGameService _gameService;

        public NhlController(INhlService nhlApiService, IGameService gameService)
        {
            _nhlService = nhlApiService;
            _gameService = gameService;
        }

        [HttpGet("schedule/{date}")]
        public async Task<IActionResult> GetScheduleByDate(string date)
        {
            var isValidDate = DateTime.TryParseExact(date, "yyyy-MM-dd", null, DateTimeStyles.None, out DateTime formattedDate);
            if (!isValidDate) return BadRequest("Invalid date");

            return Json(await _nhlService.GetScheduleByDateAsync(formattedDate));
        }

        [HttpGet("game/{gameId}")]
        public IActionResult GetGameById(int gameId)
        {
            return Json(_gameService.GetGameById(gameId));
        }
    }
}
