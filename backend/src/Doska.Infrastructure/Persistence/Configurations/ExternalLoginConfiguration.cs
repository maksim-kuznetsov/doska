using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Doska.Infrastructure.Persistence.Configurations;

/// <summary>
/// Отображение внешней идентичности: уникальность пары «провайдер + subject»
/// и одной идентичности каждого провайдера на аккаунт.
/// </summary>
public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins");

        builder.HasKey(login => login.Id);

        builder.Property(login => login.UserId).IsRequired();
        builder.Property(login => login.Provider)
            .HasMaxLength(ExternalLogin.MaxProviderLength)
            .IsRequired();
        builder.Property(login => login.Subject)
            .HasMaxLength(ExternalLogin.MaxSubjectLength)
            .IsRequired();
        builder.Property(login => login.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(login => login.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(login => new { login.Provider, login.Subject }).IsUnique();
        builder.HasIndex(login => new { login.UserId, login.Provider }).IsUnique();

        builder.HasOne(login => login.User)
            .WithMany()
            .HasForeignKey(login => login.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
