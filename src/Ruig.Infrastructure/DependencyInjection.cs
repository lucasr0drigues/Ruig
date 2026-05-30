using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ruig.Application.Common.Interfaces;
using Ruig.Application.Common.Interfaces.GitHub;
using Ruig.Application.Common.Interfaces.Strava;
using Ruig.Infrastructure.Badges;
using Ruig.Infrastructure.Common;
using Ruig.Infrastructure.Common.Persistance;
using Ruig.Infrastructure.Common.Persistance.Repositories;
using Ruig.Infrastructure.GitHub;
using Ruig.Infrastructure.Security;
using Ruig.Infrastructure.Strava;

namespace Ruig.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<StravaOptions>(configuration.GetSection(StravaOptions.SectionName));
            services.Configure<GitHubOptions>(configuration.GetSection(GitHubOptions.SectionName));
            services.Configure<TokenEncryptionOptions>(configuration.GetSection(TokenEncryptionOptions.SectionName));

            services.AddMemoryCache();

            services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            ));

            services.AddScoped<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IAthleteRepository, AthleteRepository>();
            services.AddScoped<IActivityRepository, ActivityRepository>();
            services.AddScoped<IBadgeRepository, BadgeRepository>();
            services.AddSingleton<IBadgeSlugGenerator, RandomBadgeSlugGenerator>();
            services.AddSingleton<IBadgeSvgRenderer, BadgeSvgRenderer>();
            services.AddSingleton<ITokenEncryptor, AesGcmTokenEncryptor>();
            services.AddScoped<IStravaTokenStore, StravaTokenStore>();
            services.AddScoped<IStravaActivitySyncService, StravaActivitySyncService>();
            services.AddScoped<IStravaWebhookEventStore, StravaWebhookEventStore>();
            services.AddScoped<StravaWebhookProcessor>();
            services.AddScoped<StravaActivityReconciliationService>();
            services.AddHostedService<StravaWebhookProcessingWorker>();
            services.AddHostedService<StravaActivityReconciliationWorker>();
            services.AddSingleton<IStravaOAuthStateStore, InMemoryStravaOAuthStateStore>();

            services.AddHttpClient<IStravaAuthClient, StravaAuthClient>();
            services.AddHttpClient<IStravaApiClient, StravaApiClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<StravaOptions>>().Value;
                client.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/");
            });

            services.AddHttpClient<IGitHubContributionsClient, GitHubContributionsClient>();
            services.AddScoped<IGitHubContributionsService, CachedGitHubContributionsService>();

            return services;
        }
    }
}
