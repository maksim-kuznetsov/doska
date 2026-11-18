using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doska.Infrastructure.Persistence.Configurations;

/// <summary>Отображение одноразового токена: уникальный хеш и назначение из перечисления.</summary>
public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("user_tokens", table => table.HasCheckConstraint(
            "ck_user_tokens_purpose",
            "purpose IN ('EmailConfirmation', 'PasswordReset')"));

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(token => token.Purpose).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(token => token.UsedAt).HasColumnType("timestamp with time zone");
        builder.Property(token => token.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(token => token.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder
            .HasOne(token => token.User)
            .WithMany(user => user.Tokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.Purpose });
    }
}
