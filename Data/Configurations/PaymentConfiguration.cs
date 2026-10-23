using DriveX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DriveX.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment");

        builder.HasKey(p => p.PaymentId);
        builder.Property(p => p.PaymentId).HasColumnName("payment_id").ValueGeneratedOnAdd();

        builder.Property(p => p.CarId).HasColumnName("car_id");
        builder.HasOne(p => p.Car)
               .WithMany()
               .HasForeignKey(p => p.CarId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.UserId).HasColumnName("user_id");
        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.PaymentMethod).HasColumnName("payment_method").HasConversion<string>();
        builder.Property(p => p.PaymentStatus).HasColumnName("payment_status").HasConversion<string>();
        builder.Property(p => p.PaymentType).HasColumnName("payment_type").HasConversion<string>();

        builder.Property(p => p.TotalAmount).HasColumnName("total_amount");
        builder.Property(p => p.PaymentTime).HasColumnName("payment_time");
    }
}
