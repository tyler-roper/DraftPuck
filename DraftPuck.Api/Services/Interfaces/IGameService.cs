namespace DraftPuck.Api.Services.Interfaces
{
    public interface IGameService
    {
        public Task CheckGamesAsync();
        public Game GetGameById(int id);
    }
}
