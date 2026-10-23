using DriveX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DriveX.Data.Configurations;

// Tells EF Core exactly how User lines up with the table Hibernate already created.
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.UserId);
        builder.Property(u => u.UserId).HasColumnName("user_id").ValueGeneratedOnAdd();

        builder.Property(u => u.FirstName).HasColumnName("first_name").IsRequired();
        builder.Property(u => u.LastName).HasColumnName("last_name").IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").IsRequired();
        builder.Property(u => u.Password).HasColumnName("password").IsRequired();
        builder.Property(u => u.PhoneNumber).HasColumnName("phone_number");
        builder.Property(u => u.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(u => u.ProfileImage).HasColumnName("profile_image");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");

        // The column holds text such as "ADMIN", so convert the enum to/from string.
        builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>();

        builder.Property(u => u.IsProfileComplete).HasColumnName("is_profile_complete");
    }
}
