using DraftPuck.Models.NhlApi.Helpers;

namespace DraftPuck.Models.NhlApi
{
    public class Status
    {
        public string AbstractGameState { get; set; } = null!;
        public string CodedGameState { get; set; } = null!;
        public string DetailedState { get; set; } = null!;
        public string StatusCode { get; set; } = null!;
        public bool StartTimeTBD { get; set; }

        public bool IsOver => (new[] { GameStatuses.GameOver, GameStatuses.Final, GameStatuses.Final2 }).Contains(StatusCode);
        public bool IsLive => (new[] { GameStatuses.Live, GameStatuses.LiveCritical }).Contains(StatusCode);
    }
}
