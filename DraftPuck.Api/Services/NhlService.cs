using Draftpuck.Nhl.Services.Interfaces;

namespace DraftPuck.Api.Services
{
    public class NhlService : INhlService
    {
        private readonly INhlApiService _nhlApi;
        private readonly IMapper _mapper;

        public NhlService(INhlApiService nhlApi, IMapper mapper)
        {
            _nhlApi = nhlApi;
            _mapper = mapper;
        }

        public async Task<Schedule> GetScheduleByDateAsync(DateTime date)
        {
            var schedule = await _nhlApi.GetScheduleAsync(date);
            return _mapper.Map<Schedule>(schedule);
        }

        public async Task<Player> GetPlayerAsync(int playerId)
        {
            var player = await _nhlApi.GetPlayerAsync(playerId);
            return _mapper.Map<Player>(player);
        }

        public async Task<Game> GetGameAsync(int gameId)
        {
            var game = await _nhlApi.GetFullGameAsync(gameId);
            return _mapper.Map<Game>(game);
        }
    }
}
