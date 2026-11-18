namespace Doska.Api.Infrastructure;

/// <summary>Адреса медиафайлов, отдаваемых контроллером медиа.</summary>
public static class MediaUrls
{
    /// <summary>Адрес фотографии или её уменьшенной копии.</summary>
    public static string Photo(Guid adId, string fileName)
        => $"/api/media/photos/{adId}/{fileName}";
}