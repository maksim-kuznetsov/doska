using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doska.Infrastructure.Persistence.Configurations;

/// <summary>
/// Отображение города. Города не удаляются физически: при наличии объявлений
/// связь запрещает удаление, город переводится в неактивные.
/// </summary>
public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities");

        builder.HasKey(city => city.Id);

        builder.Property(city => city.Name).HasMaxLength(200).IsRequired();
        builder.Property(city => city.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(city => city.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(city => city.Name).IsUnique();
    }
}
