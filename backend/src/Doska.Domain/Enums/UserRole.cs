namespace Doska.Domain.Enums;

/// <summary>Роль пользователя. У пользователя ровно одна роль.</summary>
public enum UserRole
{
    /// <summary>Обычный пользователь: подаёт объявления и жалобы.</summary>
    User = 0,

    /// <summary>Модератор: проверяет объявления и закрывает жалобы.</summary>
    Moderator = 1,

    /// <summary>Администратор: управляет справочниками и ролями.</summary>
    Admin = 2,
}
