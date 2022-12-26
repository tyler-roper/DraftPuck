namespace BrewPuck.Services.Interfaces
{
    public interface IGameService
    {
        public Task GetSchedule(CancellationToken cancellationToken);
    }
}
