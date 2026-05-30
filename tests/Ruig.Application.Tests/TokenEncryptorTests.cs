using Microsoft.Extensions.Options;
using Ruig.Infrastructure.Security;
using System.Security.Cryptography;

namespace Ruig.Application.Tests;

public sealed class TokenEncryptorTests
{
    [Fact]
    public void Encrypt_ProducesProtectedTextThatRoundTrips()
    {
        var encryptor = CreateEncryptor();

        var protectedText = encryptor.Encrypt("strava-token");

        Assert.StartsWith("ruig-aesgcm:v1:", protectedText);
        Assert.DoesNotContain("strava-token", protectedText);
        Assert.Equal("strava-token", encryptor.Decrypt(protectedText));
    }

    [Fact]
    public void Decrypt_WithTamperedProtectedText_Throws()
    {
        var encryptor = CreateEncryptor();
        var protectedText = encryptor.Encrypt("strava-token");
        var tampered = protectedText[..^1] + (protectedText[^1] == 'A' ? 'B' : 'A');

        Assert.ThrowsAny<CryptographicException>(() => encryptor.Decrypt(tampered));
    }

    [Fact]
    public void Decrypt_WithWrongKey_Throws()
    {
        var encryptor = CreateEncryptor();
        var wrongKeyEncryptor = CreateEncryptor(
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");

        var protectedText = encryptor.Encrypt("strava-token");

        Assert.ThrowsAny<CryptographicException>(() => wrongKeyEncryptor.Decrypt(protectedText));
    }

    [Fact]
    public void Decrypt_WithPlaintext_Throws()
    {
        var encryptor = CreateEncryptor();

        Assert.Throws<CryptographicException>(() => encryptor.Decrypt("plain-token"));
    }

    private static AesGcmTokenEncryptor CreateEncryptor(
        string key = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f")
    {
        return new AesGcmTokenEncryptor(Options.Create(new TokenEncryptionOptions
        {
            CurrentKeyId = "v1",
            Keys = new Dictionary<string, string>
            {
                ["v1"] = key
            }
        }));
    }
}
