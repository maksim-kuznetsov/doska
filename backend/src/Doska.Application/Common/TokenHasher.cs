using System.Security.Cryptography;
using System.Text;

namespace Doska.Application.Common;

/// <summary>Хеширование одноразовых токенов: хранится только хеш.</summary>
internal static class TokenHasher
{
    /// <summary>Возвращает SHA-256 хеш токена в шестнадцатеричном виде.</summary>
    public static string Hash(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}
