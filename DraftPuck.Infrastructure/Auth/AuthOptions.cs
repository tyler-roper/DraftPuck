namespace DraftPuck.Infrastructure.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";
    public string? JwtKey { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
}

