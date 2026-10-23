using DriveX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DriveX.Data.Configurations;

public class ConfirmationCodeConfiguration : IEntityTypeConfiguration<ConfirmationCode>
{
    public void Configure(EntityTypeBuilder<ConfirmationCode> builder)
    {
        builder.ToTable("conformation_code");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(c => c.Email).HasColumnName("email").IsRequired();
        builder.Property(c => c.Code).HasColumnName("code").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.ExpiresAt).HasColumnName("expires_at");
        builder.Property(c => c.IsUsed).HasColumnName("is_used");
    }
}
