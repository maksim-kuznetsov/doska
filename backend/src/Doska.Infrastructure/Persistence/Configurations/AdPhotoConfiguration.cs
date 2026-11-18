using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doska.Infrastructure.Persistence.Configurations;

/// <summary>
/// Отображение фотографии объявления: каскадное удаление вместе с объявлением
/// и структура «не более пяти фото» через номера от 1 до 5.
/// </summary>
public sealed class AdPhotoConfiguration : IEntityTypeConfiguration<AdPhoto>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AdPhoto> builder)
    {
        builder.ToTable("ad_photos", table => table.HasCheckConstraint(
            "ck_ad_photos_sort_order",
            "sort_order BETWEEN 1 AND 5"));

        builder.HasKey(photo => photo.Id);

        builder.Property(photo => photo.FileName).HasMaxLength(260).IsRequired();
        builder.Property(photo => photo.ThumbnailFileName).HasMaxLength(260).IsRequired();
        builder.Property(photo => photo.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(photo => photo.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(photo => photo.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder
            .HasOne(photo => photo.Ad)
            .WithMany(ad => ad.Photos)
            .HasForeignKey(photo => photo.AdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(photo => new { photo.AdId, photo.SortOrder }).IsUnique();
    }
}
