namespace DraftPuck.Shared.Models;
public class ErrorRequest
{
    public object Error { get; set; } = null!;
    public string Info { get; set; } = null!;
}