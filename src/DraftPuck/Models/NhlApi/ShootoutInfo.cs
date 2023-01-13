namespace DraftPuck.Models.NhlApi
{
public class ShootoutInfo
    {
        public ShootoutTeamInfo Away { get; set; } = null!;
        public ShootoutTeamInfo Home { get; set; } = null!;
    }
}