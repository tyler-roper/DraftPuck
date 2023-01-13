namespace DraftPuck.Models.NhlApi
{
public class Coach
    {
        public Person Person { get; set; } = null!;
        public Position Position { get; set; } = null!;
    }
}