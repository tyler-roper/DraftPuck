using DraftPuck.Shared.Interfaces;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Firebase;
public static class FirebaseCollectionExtensions
{
    public static IServiceCollection AddFirebase(this IServiceCollection services, Action<FirebaseOptions> configure)
    {
        services.Configure(configure);
        var serviceProvider = services.BuildServiceProvider();

        CreateFirebaseApp(serviceProvider.GetRequiredService<IOptions<FirebaseOptions>>().Value);

        return services.AddSingleton<IFirebaseService, FirebaseService>();
    }

    public static void CreateFirebaseApp(FirebaseOptions options)
    {
        try
        {
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(options.AsJson));
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromStream(stream)
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unable to connect to Firebase: {e}");
        }
    }
}
