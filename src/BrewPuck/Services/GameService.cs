using BrewPuck.Models.NHL;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BrewPuck.Services
{
    public class GameService : IGameService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEventService _eventService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<GameService> _logger;

        private List<NhlGame> Games { get; set; } = new List<NhlGame>();

        public GameService(IHttpClientFactory httpClientFactory, IEventService eventService, IServiceScopeFactory serviceScopeFactory, ILogger<GameService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _eventService = eventService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task GetSchedule(CancellationToken cancellationToken)
        {

            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BrewPuckContext>();
            if (await dbContext.Teams.CountAsync() != 32)
            {
                await AddTeams();
                await dbContext.SaveChangesAsync();
            }

            try
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://statsapi.web.nhl.com/api/v1/schedule?expand=schedule.scoringplays");

                var httpClient = _httpClientFactory.CreateClient();
                var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, cancellationToken);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken);
                    var schedule = JsonSerializer.Deserialize<Schedule>(contentStream);
                    var games = schedule?.dates.FirstOrDefault()?.games;

                    if (games == null) return;

                    HandleIncomingGames(games);
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching schedule");
            }
        }

        private void HandleIncomingGames(List<NhlGame> games)
        {
            Games = Games.Where(g => g.status.statusCode == "3" || g.status.statusCode == "4").ToList();
            var activeGames = games.Where(g => g.status.statusCode == "3" || g.status.statusCode == "4").ToList();

            activeGames.ForEach(async g =>
            {
                var previousGame = Games.FirstOrDefault(g2 => g2.gamePk == g.gamePk);
                if (previousGame == null)
                {
                    await AddGame(g);
                }
                else
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<BrewPuckContext>();
                    foreach (var scoringPlay in g.scoringPlays)
                    {
                        var scorer = scoringPlay.players.FirstOrDefault(p => p.playerType.ToLower() == "scorer");
                        var existingScoringPlay = previousGame.scoringPlays.FirstOrDefault(sp => sp.about.eventId == scoringPlay.about.eventId);
                        if (existingScoringPlay == null)
                        {
                            //new scoring play, but no scorer yet
                            if (scorer == null) return;

                            //new scoring play, scorer assigned
                            var picksForThisGame = dbContext.LobbyMemberPicks
                                .Include(lmp => lmp.LobbyMember)
                                    .ThenInclude(lm => lm.Lobby)
                                .Where(lmp => lmp.GamePk == g.gamePk && lmp.PlayerId == scorer.player.id).ToList();

                            //picksForThisGame.Select(p => p.LobbyMember.Lobby).ToList().ForEach(lobby =>
                            //{
                            //    //send notification for each lobby
                            //    _eventService.Notify(new LobbyEventModel()
                            //    {
                            //        LobbyId = lobby.Id,
                            //        Type = LobbyEventType.UserPickScored
                            //    });
                            //});
                        }
                        else
                        {
                            var prevScorer = existingScoringPlay.players.FirstOrDefault(p => p.playerType.ToLower() == "scorer");
                            if (prevScorer == null && scorer != null)
                            {
                                //update goal that previously didn't have a scorer
                            }
                            else if (prevScorer != null && scorer != null)
                            {
                                //goal scorer didn't change
                                if (prevScorer.player.id == scorer.player.id) return;

                                //goal scorer changed
                            }
                        }
                    }
                    previousGame.scoringPlays = g.scoringPlays;
                }
            });
        }

        private async Task AddGame(NhlGame game)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BrewPuckContext>();

            Games.Add(game);
            var awayTeam = game.teams.away.team;
            var homeTeam = game.teams.home.team;

            if (await dbContext.Games.AnyAsync(g => g.GamePk == game.gamePk)) return;

            dbContext.Games.Add(new Game()
            {
                GamePk = game.gamePk,
                AwayTeamId = awayTeam.id,
                HomeTeamId = homeTeam.id,
                Type = game.gameType,
                StatusCode = (GameStatus)int.Parse(game.status.statusCode),
                Date = game.gameDate
            });

            await dbContext.SaveChangesAsync();
        }

        private async Task AddTeams()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://statsapi.web.nhl.com/api/v1/teams");

            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
                var teams = JsonSerializer.Deserialize<TeamsResponse>(contentStream);

                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BrewPuckContext>();

                foreach (FullTeam team in teams.teams)
                {
                    dbContext.Teams.Add(new Data.Team()
                    {
                        Id = team.id,
                        TeamName = team.teamName,
                        Abbreviation = team.abbreviation,
                        ShortName = team.shortName,
                        LocationName = team.locationName
                    });
                }

                await dbContext.SaveChangesAsync();
            }
        }

        private static bool AnyActiveGames(dynamic games)
        {
            foreach (dynamic game in games)
            {
                if (game["status"]["statusCode"] == "3" || game["status"]["statusCode"] == "4")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
