using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Firebase;
public static class FirebaseCollectionExtensions
{
    //public static IServiceCollection AddFirebase(this IServiceCollection services, Action<FirebaseOptions> configure) =>
    //    services
    //        .Configure(configure)
    //        .AddSingleton(serviceProvider => CreateFirebaseApp(serviceProvider.GetRequiredService<IOptions<FirebaseOptions>>().Value))
    //        .AddSingleton<IFirebaseService, FirebaseService();

    //public static FirebaseApp CreateFirebaseApp(FirebaseOptions options)
    //{
    //    var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(options.AsJson));
    //    return FirebaseApp.Create(new AppOptions()
    //    {
    //        Credential = GoogleCredential.FromStream(stream)
    //    });
    //}
}
