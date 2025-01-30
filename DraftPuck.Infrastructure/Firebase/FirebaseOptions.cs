using System.Text.Json;

namespace DraftPuck.Infrastructure.Firebase;
public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    private static readonly JsonSerializerOptions Serializer = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public string? Type { get; set; }
    public string? ProjectId { get; set; }
    public string? PrivateKeyId { get; set; }
    public string? PrivateKey { get; set; }
    public string? ClientEmail { get; set; }
    public string? AuthUri { get; set; }
    public string? TokenUri { get; set; }
    public string? AutoProviderX509CertUrl { get; set; }
    public string? ClientX509CertUrl { get; set; }
    public string? UniverseDomain { get; set; }

    public string AsJson => JsonSerializer.Serialize(this, Serializer);
}