namespace Ruig.Infrastructure.Security
{
    public sealed class TokenEncryptionOptions
    {
        public const string SectionName = "TokenEncryption";

        public string CurrentKeyId { get; init; } = string.Empty;

        public Dictionary<string, string> Keys { get; init; } = new();
    }
}
