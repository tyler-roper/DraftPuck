namespace DraftPuck.NhlApi.Models
{
    public class Player
    {
        public int TeamId { get; set; }
        public int PlayerId { get; set; }
        public DefaultString FirstName { get; set; } = null!;
        public DefaultString LastName { get; set; } = null!;
        public int SweaterNumber { get; set; }
        public string PositionCode { get; set; } = null!;
        public string Headshot { get; set; } = null!;
    }


}
