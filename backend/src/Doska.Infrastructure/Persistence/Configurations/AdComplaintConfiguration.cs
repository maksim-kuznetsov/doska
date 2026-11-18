using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doska.Infrastructure.Persistence.Configurations;

/// <summary>
/// Отображение жалобы: каскадное удаление вместе с объявлением, запрет удаления
/// автора жалобы и закрывающего модератора, одна жалоба пользователя на объявление.
/// </summary>
public sealed class AdComplaintConfiguration : IEntityTypeConfiguration<AdComplaint>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AdComplaint> builder)
    {
        builder.ToTable("ad_complaints", table => table.HasCheckConstraint(
            "ck_ad_complaints_status",
            "status IN ('Open', 'Closed')"));

        builder.HasKey(complaint => complaint.Id);

        builder.Property(complaint => complaint.Reason).HasColumnType("text").IsRequired();
        builder.Property(complaint => complaint.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(complaint => complaint.ClosedAt).HasColumnType("timestamp with time zone");
        builder.Property(complaint => complaint.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(complaint => complaint.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder
            .HasOne(complaint => complaint.Ad)
            .WithMany(ad => ad.Complaints)
            .HasForeignKey(complaint => complaint.AdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(complaint => complaint.Author)
            .WithMany(user => user.Complaints)
            .HasForeignKey(complaint => complaint.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(complaint => complaint.ClosedByUser)
            .WithMany()
            .HasForeignKey(complaint => complaint.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(complaint => new { complaint.AdId, complaint.AuthorId }).IsUnique();
        builder.HasIndex(complaint => new { complaint.Status, complaint.CreatedAt });
    }
}
