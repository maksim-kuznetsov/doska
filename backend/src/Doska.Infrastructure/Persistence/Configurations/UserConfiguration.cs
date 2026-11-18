using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doska.Infrastructure.Persistence.Configurations;

/// <summary>Отображение пользователя: уникальная почта и ограничение на значения роли.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table => table.HasCheckConstraint(
            "ck_users_role",
            "role IN ('User', 'Moderator', 'Admin')"));

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.BlockedAt).HasColumnType("timestamp with time zone");
        builder.Property(user => user.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(user => user.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();
    }
}
