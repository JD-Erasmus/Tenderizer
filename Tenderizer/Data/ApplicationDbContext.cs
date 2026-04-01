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
        public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
        public DbSet<LibraryDocument> LibraryDocuments => Set<LibraryDocument>();
        public DbSet<LibraryDocumentVersion> LibraryDocumentVersions => Set<LibraryDocumentVersion>();
        public DbSet<TenderDocument> TenderDocuments => Set<TenderDocument>();
        public DbSet<TenderDocumentCvMetadata> TenderDocumentCvMetadata => Set<TenderDocumentCvMetadata>();

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

                entity.HasMany(x => x.Documents)
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

            builder.Entity<StoredFile>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.StorageProvider)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.RelativePath)
                    .HasMaxLength(260)
                    .IsRequired();

                entity.Property(x => x.StoredFileName)
                    .HasMaxLength(260)
                    .IsRequired();

                entity.Property(x => x.OriginalFileName)
                    .HasMaxLength(260)
                    .IsRequired();

                entity.Property(x => x.ContentType)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Sha256)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.Property(x => x.UploadedByUserId)
                    .IsRequired();

                entity.HasIndex(x => x.RelativePath)
                    .IsUnique();
            });

            builder.Entity<LibraryDocument>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.CreatedByUserId)
                    .IsRequired();

                entity.HasIndex(x => x.Name)
                    .IsUnique();

                entity.HasMany(x => x.Versions)
                    .WithOne(x => x.LibraryDocument)
                    .HasForeignKey(x => x.LibraryDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<LibraryDocumentVersion>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CreatedByUserId)
                    .IsRequired();

                entity.HasIndex(x => new { x.LibraryDocumentId, x.VersionNumber })
                    .IsUnique();

                entity.HasIndex(x => new { x.LibraryDocumentId, x.IsCurrent })
                    .HasFilter("[IsCurrent] = 1")
                    .IsUnique();

                entity.HasOne(x => x.StoredFile)
                    .WithMany(x => x.LibraryDocumentVersions)
                    .HasForeignKey(x => x.StoredFileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TenderDocument>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.DisplayName)
                    .HasMaxLength(260)
                    .IsRequired();

                entity.Property(x => x.AttachedByUserId)
                    .IsRequired();

                entity.HasIndex(x => new { x.TenderId, x.Category, x.AttachedAtUtc });

                entity.HasOne(x => x.StoredFile)
                    .WithMany(x => x.TenderDocuments)
                    .HasForeignKey(x => x.StoredFileId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.LibraryDocumentVersion)
                    .WithMany(x => x.TenderDocuments)
                    .HasForeignKey(x => x.LibraryDocumentVersionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CvMetadata)
                    .WithOne(x => x.TenderDocument)
                    .HasForeignKey<TenderDocumentCvMetadata>(x => x.TenderDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TenderDocumentCvMetadata>(entity =>
            {
                entity.HasKey(x => x.TenderDocumentId);

                entity.Property(x => x.PersonName)
                    .HasMaxLength(200);

                entity.Property(x => x.ProjectRole)
                    .HasMaxLength(200);
            });
        }
    }
}
