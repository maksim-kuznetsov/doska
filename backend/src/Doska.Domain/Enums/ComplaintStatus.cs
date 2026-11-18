namespace Doska.Domain.Enums;

/// <summary>Состояние жалобы на объявление.</summary>
public enum ComplaintStatus
{
    /// <summary>Открыта: ожидает решения модератора.</summary>
    Open = 0,

    /// <summary>Закрыта: модератор вынес решение.</summary>
    Closed = 1,
}
