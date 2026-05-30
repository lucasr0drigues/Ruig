using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

namespace Ruig.Api.Configuration
{
    public static partial class ProductionConfigurationValidator
    {
        public static void Validate(IConfiguration configuration, string environmentName)
        {
            if (!string.Equals(environmentName, Environments.Production, StringComparison.OrdinalIgnoreCase))
                return;

            var errors = new List<string>();

            Require(configuration, "AllowedHosts", errors);
            Require(configuration, "ConnectionStrings:Default", errors);
            Require(configuration, "Strava:ClientId", errors);
            Require(configuration, "Strava:ClientSecret", errors);
            Require(configuration, "Strava:RedirectUri", errors);
            Require(configuration, "Strava:WebhookVerifyToken", errors);
            Require(configuration, "GitHub:AccessToken", errors);
            Require(configuration, "TokenEncryption:CurrentKeyId", errors);

            ValidateAllowedHosts(configuration["AllowedHosts"], errors);
            ValidateRedirectUri(configuration["Strava:RedirectUri"], errors);
            ValidateWebhookSubscriptionId(configuration["Strava:WebhookSubscriptionId"], errors);
            ValidateTokenEncryptionKey(configuration, errors);

            if (errors.Count != 0)
            {
                throw new InvalidOperationException(
                    "Production configuration is invalid: " + string.Join("; ", errors));
            }
        }

        private static void Require(IConfiguration configuration, string key, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(configuration[key]))
                errors.Add($"{key} is required");
        }

        private static void ValidateAllowedHosts(string? allowedHosts, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(allowedHosts))
                return;

            if (string.Equals(allowedHosts.Trim(), "*", StringComparison.Ordinal))
                errors.Add("AllowedHosts must not be '*' in Production");
        }

        private static void ValidateRedirectUri(string? redirectUri, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
                return;

            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            {
                errors.Add("Strava:RedirectUri must be an absolute URI");
                return;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                errors.Add("Strava:RedirectUri must use HTTPS in Production");

            if (uri.IsLoopback)
                errors.Add("Strava:RedirectUri must not point to localhost in Production");
        }

        private static void ValidateWebhookSubscriptionId(string? subscriptionId, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                errors.Add("Strava:WebhookSubscriptionId is required");
                return;
            }

            if (!long.TryParse(subscriptionId, out var parsed) || parsed <= 0)
                errors.Add("Strava:WebhookSubscriptionId must be a positive integer");
        }

        private static void ValidateTokenEncryptionKey(IConfiguration configuration, List<string> errors)
        {
            var currentKeyId = configuration["TokenEncryption:CurrentKeyId"];
            if (string.IsNullOrWhiteSpace(currentKeyId))
                return;

            var key = configuration[$"TokenEncryption:Keys:{currentKeyId}"];
            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add($"TokenEncryption:Keys:{currentKeyId} is required");
                return;
            }

            if (key.Length != 64 || !HexRegex().IsMatch(key))
                errors.Add($"TokenEncryption:Keys:{currentKeyId} must be a 64-character hex encoded 256-bit key");
        }

        [GeneratedRegex("^[0-9a-fA-F]+$")]
        private static partial Regex HexRegex();
    }
}
