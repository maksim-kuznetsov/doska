using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doska.Infrastructure.Persistence.Configurations;

/// <summary>
/// Отображение объявления: цена в <c>numeric(12,2)</c>, состояния строкой,
/// запрет удаления автора, категории и города при наличии объявлений, индексы ленты.
/// </summary>
public sealed class AdConfiguration : IEntityTypeConfiguration<Ad>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ad> builder)
    {
        builder.ToTable("ads", table =>
        {
            table.HasCheckConstraint("ck_ads_price_non_negative", "price >= 0");
            table.HasCheckConstraint(
                "ck_ads_status",
                "status IN ('OnModeration', 'Active', 'Sold', 'Archived', 'Rejected', 'Hidden')");
        });

        builder.HasKey(ad => ad.Id);

        builder.Property(ad => ad.Title).HasMaxLength(200).IsRequired();
        builder.Property(ad => ad.Description).HasColumnType("text").IsRequired();
        builder.Property(ad => ad.Price).HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(ad => ad.Contacts).HasMaxLength(500).IsRequired();
        builder.Property(ad => ad.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(ad => ad.ModerationReason).HasColumnType("text");
        builder.Property(ad => ad.PublishedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(ad => ad.ReminderSentAt).HasColumnType("timestamp with time zone");
        builder.Property(ad => ad.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(ad => ad.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder
            .HasOne(ad => ad.Author)
            .WithMany(user => user.Ads)
            .HasForeignKey(ad => ad.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(ad => ad.Category)
            .WithMany(category => category.Ads)
            .HasForeignKey(ad => ad.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(ad => ad.City)
            .WithMany(city => city.Ads)
            .HasForeignKey(ad => ad.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ad => new { ad.Status, ad.PublishedAt }).IsDescending(false, true);
        builder.HasIndex(ad => new { ad.CategoryId, ad.Status });
        builder.HasIndex(ad => ad.CityId);
        builder.HasIndex(ad => new { ad.AuthorId, ad.CreatedAt });
    }
}
