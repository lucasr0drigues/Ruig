namespace Ruig.Infrastructure.Security
{
    public interface ITokenEncryptor
    {
        string Encrypt(string plaintext);

        string Decrypt(string protectedText);
    }
}
