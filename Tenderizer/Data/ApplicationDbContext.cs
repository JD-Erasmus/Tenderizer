using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tenderizer.Models;

namespace Tenderizer.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tender> Tenders => Set<Tender>();
        public DbSet<TenderReminder> TenderReminders => Set<TenderReminder>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Tender>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ReferenceNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.Client)
                    .HasMaxLength(200);

                entity.Property(x => x.Category)
                    .HasMaxLength(100);

                entity.Property(x => x.ClosingAtUtc)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .IsRequired();

                entity.Property(x => x.OwnerUserId)
                    .IsRequired();

                entity.HasIndex(x => x.ClosingAtUtc);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.OwnerUserId);

                entity.HasMany(x => x.Reminders)
                    .WithOne(x => x.Tender)
                    .HasForeignKey(x => x.TenderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TenderReminder>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.ReminderAtUtc)
                    .IsRequired();

                entity.Property(x => x.AttemptCount)
                    .HasDefaultValue(0);

                entity.Property(x => x.LastError)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.SentAtUtc, x.ReminderAtUtc });

                entity.HasIndex(x => new { x.TenderId, x.ReminderAtUtc })
                    .IsUnique();
            });
        }
    }
}
