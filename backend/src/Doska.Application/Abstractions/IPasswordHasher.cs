namespace Doska.Application.Abstractions;

/// <summary>Хеширование и проверка паролей.</summary>
public interface IPasswordHasher
{
    /// <summary>Формирует хеш пароля для хранения.</summary>
    string Hash(string password);

    /// <summary>Проверяет пароль по сохранённому хешу.</summary>
    bool Verify(string password, string hash);
}
