using DriveX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DriveX.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.BookingId);
        builder.Property(b => b.BookingId).HasColumnName("booking_id").ValueGeneratedOnAdd();

        builder.Property(b => b.UserId).HasColumnName("user_id");
        builder.HasOne(b => b.User)
               .WithMany()
               .HasForeignKey(b => b.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        // One booking has exactly one payment.
        builder.Property(b => b.PaymentId).HasColumnName("payment_id");
        builder.HasOne(b => b.Payment)
               .WithOne()
               .HasForeignKey<Booking>(b => b.PaymentId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.CarId).HasColumnName("car_id");
        builder.HasOne(b => b.Car)
               .WithMany()
               .HasForeignKey(b => b.CarId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.TotalPrice).HasColumnName("total_price");
        builder.Property(b => b.BookingTime).HasColumnName("booking_time");
        builder.Property(b => b.Days).HasColumnName("days");
        builder.Property(b => b.Hour).HasColumnName("hour");
        builder.Property(b => b.TotalHours).HasColumnName("total_hours");
        builder.Property(b => b.Address).HasColumnName("address").IsRequired();
        builder.Property(b => b.Phone).HasColumnName("phone").IsRequired();

        builder.Property(b => b.BookingStatus).HasColumnName("booking_status").HasConversion<string>();

        // These three had no @Column in Java, so Hibernate named them from the field names.
        builder.Property(b => b.RentStart).HasColumnName("rent_start");
        builder.Property(b => b.RentEnd).HasColumnName("rent_end");
        builder.Property(b => b.Toggle).HasColumnName("toggle");
    }
}
