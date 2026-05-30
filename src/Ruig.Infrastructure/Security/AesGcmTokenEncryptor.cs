using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Ruig.Infrastructure.Security
{
    public sealed class AesGcmTokenEncryptor : ITokenEncryptor
    {
        private const string FormatPrefix = "ruig-aesgcm";
        private const int KeySizeBytes = 32;
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;

        private readonly string _currentKeyId;
        private readonly IReadOnlyDictionary<string, byte[]> _keysById;

        public AesGcmTokenEncryptor(IOptions<TokenEncryptionOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var value = options.Value;
            if (string.IsNullOrWhiteSpace(value.CurrentKeyId))
                throw new InvalidOperationException("Token encryption current key id is not configured.");

            if (value.Keys.Count == 0)
                throw new InvalidOperationException("Token encryption keys are not configured.");

            _keysById = value.Keys.ToDictionary(
                kvp => kvp.Key,
                kvp => ParseKey(kvp.Key, kvp.Value),
                StringComparer.Ordinal);

            if (!_keysById.ContainsKey(value.CurrentKeyId))
                throw new InvalidOperationException("Token encryption current key id does not have a configured key.");

            _currentKeyId = value.CurrentKeyId;
        }

        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrWhiteSpace(plaintext))
                throw new ArgumentException("Token plaintext is required.", nameof(plaintext));

            var key = _keysById[_currentKeyId];
            var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSizeBytes];
            var associatedData = BuildAssociatedData(_currentKeyId);

            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, associatedData);

            return string.Join(
                ":",
                FormatPrefix,
                _currentKeyId,
                Base64UrlEncode(nonce),
                Base64UrlEncode(ciphertext),
                Base64UrlEncode(tag));
        }

        public string Decrypt(string protectedText)
        {
            if (string.IsNullOrWhiteSpace(protectedText))
                throw new ArgumentException("Protected token text is required.", nameof(protectedText));

            var parts = protectedText.Split(':');
            if (parts.Length != 5 || !string.Equals(parts[0], FormatPrefix, StringComparison.Ordinal))
                throw new CryptographicException("Protected token text has an invalid format.");

            var keyId = parts[1];
            if (!_keysById.TryGetValue(keyId, out var key))
                throw new CryptographicException("Protected token text references an unknown key.");

            var nonce = Base64UrlDecode(parts[2]);
            var ciphertext = Base64UrlDecode(parts[3]);
            var tag = Base64UrlDecode(parts[4]);

            if (nonce.Length != NonceSizeBytes || tag.Length != TagSizeBytes)
                throw new CryptographicException("Protected token text has an invalid format.");

            var plaintext = new byte[ciphertext.Length];
            var associatedData = BuildAssociatedData(keyId);

            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

            return Encoding.UTF8.GetString(plaintext);
        }

        private static byte[] ParseKey(string keyId, string encodedKey)
        {
            if (string.IsNullOrWhiteSpace(keyId))
                throw new InvalidOperationException("Token encryption key id is empty.");

            if (string.IsNullOrWhiteSpace(encodedKey))
                throw new InvalidOperationException($"Token encryption key '{keyId}' is empty.");

            byte[] key;
            try
            {
                key = Convert.FromHexString(encodedKey);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"Token encryption key '{keyId}' must be hex encoded.", ex);
            }

            if (key.Length != KeySizeBytes)
                throw new InvalidOperationException($"Token encryption key '{keyId}' must be {KeySizeBytes} bytes.");

            return key;
        }

        private static byte[] BuildAssociatedData(string keyId)
        {
            return Encoding.UTF8.GetBytes($"{FormatPrefix}:{keyId}");
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string value)
        {
            var base64 = value
                .Replace('-', '+')
                .Replace('_', '/');

            base64 = (base64.Length % 4) switch
            {
                0 => base64,
                2 => base64 + "==",
                3 => base64 + "=",
                _ => throw new CryptographicException("Protected token text has an invalid format.")
            };

            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("Protected token text has an invalid format.", ex);
            }
        }
    }
}
