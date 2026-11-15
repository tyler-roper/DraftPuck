using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));

        return services.AddSingleton<IEmailSender>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            return string.IsNullOrEmpty(options.ConnectionString)
                ? new LoggingEmailSender(sp.GetRequiredService<ILogger<LoggingEmailSender>>())
                : new AzureEmailSender(new EmailClient(options.ConnectionString), options.SenderAddress);
        });
    }
}
