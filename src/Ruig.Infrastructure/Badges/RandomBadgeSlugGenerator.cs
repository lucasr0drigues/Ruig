using Ruig.Application.Common.Interfaces;
using System;
using System.Security.Cryptography;

namespace Ruig.Infrastructure.Badges
{
    public sealed class RandomBadgeSlugGenerator : IBadgeSlugGenerator
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int SlugLength = 12;

        public string Generate()
        {
            Span<char> buffer = stackalloc char[SlugLength];

            for (var i = 0; i < SlugLength; i++)
            {
                var index = RandomNumberGenerator.GetInt32(Alphabet.Length);
                buffer[i] = Alphabet[index];
            }

            return new string(buffer);
        }
    }
}
