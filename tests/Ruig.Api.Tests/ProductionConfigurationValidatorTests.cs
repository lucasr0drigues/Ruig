using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Ruig.Api.Configuration;

namespace Ruig.Api.Tests;

public sealed class ProductionConfigurationValidatorTests
{
    [Fact]
    public void Validate_WithDevelopmentEnvironment_DoesNotRequireProductionSettings()
    {
        var configuration = new ConfigurationBuilder().Build();

        ProductionConfigurationValidator.Validate(configuration, Environments.Development);
    }

    [Fact]
    public void Validate_WithValidProductionSettings_DoesNotThrow()
    {
        var configuration = BuildConfiguration();

        ProductionConfigurationValidator.Validate(configuration, Environments.Production);
    }

    [Fact]
    public void Validate_WithMissingProductionSettings_ThrowsWithoutSecretValues()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "*",
            ["Strava:RedirectUri"] = "https://localhost/auth/strava/callback",
            ["Strava:WebhookSubscriptionId"] = "0",
            ["TokenEncryption:Keys:v1"] = "not-a-key"
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, Environments.Production));

        Assert.Contains("AllowedHosts must not be '*'", ex.Message);
        Assert.Contains("Strava:RedirectUri must not point to localhost", ex.Message);
        Assert.Contains("Strava:WebhookSubscriptionId must be a positive integer", ex.Message);
        Assert.Contains("TokenEncryption:Keys:v1 must be a 64-character", ex.Message);
        Assert.DoesNotContain("ghp_secret", ex.Message);
        Assert.DoesNotContain("strava-secret", ex.Message);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "ruig.example",
            ["ConnectionStrings:Default"] = "Host=localhost;Database=ruig;Username=ruig;Password=db-secret",
            ["Strava:ClientId"] = "client-id",
            ["Strava:ClientSecret"] = "strava-secret",
            ["Strava:RedirectUri"] = "https://ruig.example/auth/strava/callback",
            ["Strava:WebhookVerifyToken"] = "verify-token",
            ["Strava:WebhookSubscriptionId"] = "456",
            ["GitHub:AccessToken"] = "ghp_secret",
            ["TokenEncryption:CurrentKeyId"] = "v1",
            ["TokenEncryption:Keys:v1"] = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                values[key] = value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
