namespace Draftpuck.NhlApi.Models;

public class NhlTvBroadcast
{
    public int Id { get; set; }
    public string Market { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
    public string Network { get; set; } = null!;
}
