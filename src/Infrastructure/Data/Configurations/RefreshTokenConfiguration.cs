using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Identity;

namespace ProductManagement.Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    // Matches the key length of the Identity user table.
    private const int UserIdMaxLength = 450;

    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.UserId)
            .IsRequired()
            .HasMaxLength(UserIdMaxLength);

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(RefreshToken.TokenHashMaxLength);

        builder.Property(token => token.ReplacedByTokenHash)
            .HasMaxLength(RefreshToken.TokenHashMaxLength);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasIndex(token => token.UserId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
