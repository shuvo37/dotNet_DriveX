using DriveX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DriveX.Data.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submission");

        builder.HasKey(s => s.SubmissionId);
        builder.Property(s => s.SubmissionId).HasColumnName("submission_id").ValueGeneratedOnAdd();

        builder.Property(s => s.PaymentId).HasColumnName("payment_id");
        builder.HasOne(s => s.Payment)
               .WithOne()
               .HasForeignKey<Submission>(s => s.PaymentId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.BookingId).HasColumnName("booking_id");
        builder.HasOne(s => s.Booking)
               .WithOne()
               .HasForeignKey<Submission>(s => s.BookingId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.CarId).HasColumnName("car_id");
        builder.HasOne(s => s.Car)
               .WithMany()
               .HasForeignKey(s => s.CarId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        // Careful: in this table the user column is "userid", not "user_id".
        builder.Property(s => s.UserId).HasColumnName("userid");
        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.SubmittedAt).HasColumnName("submitted_at");
    }
}
