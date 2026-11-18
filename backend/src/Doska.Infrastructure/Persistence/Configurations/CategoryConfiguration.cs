using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doska.Infrastructure.Persistence.Configurations;

/// <summary>
/// Отображение категории: самоссылка «раздел → подраздел» с запретом удаления родителя
/// и ограничением порядка вывода.
/// </summary>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", table => table.HasCheckConstraint(
            "ck_categories_sort_order",
            "sort_order >= 0"));

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name).HasMaxLength(200).IsRequired();
        builder.Property(category => category.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(category => category.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder
            .HasOne(category => category.Parent)
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(category => category.Name).IsUnique();
    }
}
