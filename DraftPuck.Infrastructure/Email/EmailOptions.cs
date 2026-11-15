namespace DraftPuck.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>The Azure Communication Services connection string. Without it, emails are logged, not sent.</summary>
    public string? ConnectionString { get; set; }

    public string SenderAddress { get; set; } = "DoNotReply@draftpuck.com";
}
