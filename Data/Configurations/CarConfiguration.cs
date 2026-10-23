using DriveX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DriveX.Data.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.ToTable("cars");

        builder.HasKey(c => c.CarId);
        builder.Property(c => c.CarId).HasColumnName("car_id").ValueGeneratedOnAdd();

        // cars.company_id points at companies.company_id
        builder.Property(c => c.CompanyId).HasColumnName("company_id");
        builder.HasOne(c => c.Company)
               .WithMany(co => co.Cars)
               .HasForeignKey(c => c.CompanyId)
               .IsRequired();

        builder.Property(c => c.ModelName).HasColumnName("model_name").IsRequired();
        builder.Property(c => c.PricePerHour).HasColumnName("price_per_hour");
        builder.Property(c => c.Seats).HasColumnName("seats");
        builder.Property(c => c.ImageUrl).HasColumnName("image_url");
        builder.Property(c => c.Tag).HasColumnName("tag");
        builder.Property(c => c.IsActive).HasColumnName("is_active");

        // Both columns hold text such as "AVAILABLE" or "PETROL".
        builder.Property(c => c.RentalStatus).HasColumnName("rental_status").HasConversion<string>();
        builder.Property(c => c.Fuel).HasColumnName("fuel").HasConversion<string>();
    }
}
