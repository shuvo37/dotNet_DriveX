using DriveX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DriveX.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.CompanyId);
        builder.Property(c => c.CompanyId).HasColumnName("company_id").ValueGeneratedOnAdd();

        builder.Property(c => c.CompanyName).HasColumnName("company_name").IsRequired();
        builder.Property(c => c.CompanyEmail).HasColumnName("company_email").IsRequired();
        builder.Property(c => c.CompanyPhoneNumber).HasColumnName("company_phone_number").IsRequired();
        builder.Property(c => c.CompanyCity).HasColumnName("company_city").IsRequired();
        builder.Property(c => c.CompanyAddress).HasColumnName("company_address").IsRequired();
        builder.Property(c => c.IsActive).HasColumnName("is_active");
    }
}
