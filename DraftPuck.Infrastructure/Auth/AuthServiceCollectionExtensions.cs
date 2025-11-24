using DraftPuck.Infrastructure.Nhl;
using DraftPuck.Shared.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DraftPuck.Infrastructure.Auth
{
    public static class AuthServiceCollectionExtensions
    {
        public static IServiceCollection AddTokenService(this IServiceCollection services, IConfiguration configuration) =>
            services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName))
                .AddTransient<ITokenService>(serviceProvider =>
                    CreateTokenService(serviceProvider.GetRequiredService<IOptions<AuthOptions>>().Value)
                );

        public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()!;
            services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

            services.AddAuthentication(options =>
             {
                 options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                 options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
             })
            .AddJwtBearer(options =>
            {
                var key = Encoding.UTF8.GetBytes(authOptions!.JwtKey!);

                options.RequireHttpsMetadata = env.IsProduction();
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services
                .AddAuthorizationBuilder()
                .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));

            return services;
        }

        private static TokenService CreateTokenService(AuthOptions authOptions)
        {
            if (string.IsNullOrEmpty(authOptions.JwtKey))
                throw new InvalidOperationException("JWT Key is missing in AuthOptions configuration.");

            if (string.IsNullOrEmpty(authOptions.Issuer))
                throw new InvalidOperationException("JWT Issuer is missing in AuthOptions configuration.");

            if (string.IsNullOrEmpty(authOptions.Audience))
                throw new InvalidOperationException("JWT Audience is missing in AuthOptions configuration.");

            return new TokenService(authOptions);
        }
    }
}
