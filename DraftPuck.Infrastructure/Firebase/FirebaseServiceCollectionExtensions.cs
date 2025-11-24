using DraftPuck.Shared.Firebase;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Firebase;

public static class FirebaseServiceCollectionExtensions
{
    public static IServiceCollection AddFirebase(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName))
            .AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<FirebaseOptions>>().Value;
                return CreateFirebaseApp(options);
            })
            .AddSingleton<IPushNotificationService, FirebaseService>();
    }

    private static FirebaseApp CreateFirebaseApp(FirebaseOptions options)
    {
        var json = options.AsJson;
        return FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(json)
        });
    }
}
