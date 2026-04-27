using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Implementations;
using Tenderizer.Services.Interfaces;

namespace TenderizerTest;

public sealed class TenderServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenNonAdmin_UsesCurrentUserAsOwner_AndSetsAuditFields()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var userId = "user-1";
        var dto = new TenderUpsertDto
        {
            Name = "  Tender A  ",
            OwnerUserId = "someone-else",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(10),
            Status = TenderStatus.Draft,
        };

        var id = await service.CreateAsync(dto, userId, isAdmin: false);

        var entity = await db.Tenders.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal("Tender A", entity.Name);
        Assert.Equal(userId, entity.OwnerUserId);
        Assert.True(entity.CreatedAtUtc != default);
        Assert.Equal(entity.CreatedAtUtc, entity.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_WhenAdmin_UsesDtoOwnerUserId()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var dto = new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner-123",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(2),
            Status = TenderStatus.Identified,
        };

        var id = await service.CreateAsync(dto, userId: "admin", isAdmin: true);

        var entity = await db.Tenders.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal("owner-123", entity.OwnerUserId);
    }

    [Fact]
    public async Task CreateAsync_WhenNonAdminAndClosingInPast_Throws()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var dto = new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "ignored",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = TenderStatus.Draft,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto, userId: "u1", isAdmin: false));
    }

    [Fact]
    public async Task CreateAsync_WhenAdminAndClosingInPast_DoesNotThrow()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var dto = new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = TenderStatus.Submitted,
        };

        var id = await service.CreateAsync(dto, userId: "admin", isAdmin: true);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task UpdateAsync_WhenNonOwnerNonAdmin_ThrowsUnauthorized()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var id = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Draft,
        }, userId: "owner", isAdmin: false);

        var dto = new TenderUpsertDto
        {
            Name = "Tender A2",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(6),
            Status = TenderStatus.Draft,
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateAsync(id, dto, userId: "intruder", isAdmin: false));
    }

    [Fact]
    public async Task UpdateAsync_WhenOwner_UpdatesFieldsAndUpdatedAt()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var id = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Draft,
            ReferenceNumber = " r1 ",
        }, userId: "owner", isAdmin: false);

        var before = await db.Tenders.AsNoTracking().SingleAsync(t => t.Id == id);

        var dto = new TenderUpsertDto
        {
            Name = "  Tender A2  ",
            OwnerUserId = "ignored",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(6),
            Status = TenderStatus.InProgress,
            ReferenceNumber = " r2 ",
            Client = " c ",
            Category = TenderCategory.Other,
        };

        await service.UpdateAsync(id, dto, userId: "owner", isAdmin: false);

        var after = await db.Tenders.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal("Tender A2", after.Name);
        Assert.Equal("r2", after.ReferenceNumber);
        Assert.Equal("c", after.Client);
        Assert.Equal(TenderCategory.Other.ToStorageValue(), after.Category);
        Assert.Equal(TenderStatus.InProgress, after.Status);
        Assert.Equal("owner", after.OwnerUserId);
        Assert.True(after.UpdatedAtUtc > before.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenNonOwnerNonAdmin_ReturnsDetails()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var id = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Draft,
        }, userId: "owner", isAdmin: false);

        var details = await service.GetDetailsAsync(id, userId: "other", isAdmin: false);

        Assert.Equal(id, details.Id);
        Assert.Equal("owner", details.OwnerUserId);
    }

    [Fact]
    public async Task GetListAsync_IncludesOwnerDisplayName()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        db.Users.Add(new IdentityUser
        {
            Id = "owner",
            UserName = "owner@local.test",
            NormalizedUserName = "OWNER@LOCAL.TEST",
            Email = "owner@local.test",
            NormalizedEmail = "OWNER@LOCAL.TEST",
        });

        var service = CreateService(db);

        var id = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Draft,
        }, userId: "admin", isAdmin: true);

        var item = Assert.Single(await service.GetListAsync());

        Assert.Equal(id, item.Id);
        Assert.Equal("owner@local.test", item.OwnerDisplayName);
        Assert.True(item.UpdatedAtUtc != default);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenOwner_UpdatesStatusAndClearsPendingReminders()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var id = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Draft,
        }, userId: "owner", isAdmin: false);

        var before = await db.Tenders.AsNoTracking().SingleAsync(t => t.Id == id);
        await Task.Delay(20);

        await service.UpdateStatusAsync(id, TenderStatus.Submitted, userId: "owner", isAdmin: false);

        var after = await db.Tenders.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal(TenderStatus.Submitted, after.Status);
        Assert.True(after.UpdatedAtUtc > before.UpdatedAtUtc);
        Assert.False(await db.TenderReminders.AsNoTracking().AnyAsync(r => r.TenderId == id && r.SentAtUtc == null));
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenNonOwnerNonAdmin_ThrowsUnauthorized()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var id = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Draft,
        }, userId: "owner", isAdmin: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateStatusAsync(id, TenderStatus.Won, userId: "intruder", isAdmin: false));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTender()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = CreateService(db);

        var id = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Draft,
        }, userId: "owner", isAdmin: false);

        await service.DeleteAsync(id);

        Assert.False(await db.Tenders.AsNoTracking().AnyAsync(t => t.Id == id));
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenDraftToIdentified_GeneratesChecklistAndSendsStatusNotification()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var checklist = new RecordingChecklistService();
        var notifications = new RecordingNotificationService();
        var service = CreateService(db, checklist, notifications);

        var tenderId = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Draft,
        }, userId: "owner", isAdmin: false);

        await service.UpdateStatusAsync(tenderId, TenderStatus.Identified, userId: "owner", isAdmin: false);

        Assert.Contains(tenderId, checklist.GeneratedTenderIds);
        var statusNotification = Assert.Single(notifications.StatusChanges);
        Assert.Equal(tenderId, statusNotification.TenderId);
        Assert.Equal(TenderStatus.Draft, statusNotification.From);
        Assert.Equal(TenderStatus.Identified, statusNotification.To);
    }

    [Fact]
    public async Task CreateAsync_WhenAssignedUsersProvided_SendsAssignmentAndInitialStatusNotifications()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        db.Users.AddRange(
            new IdentityUser
            {
                Id = "user-1",
                UserName = "user1@local.test",
                NormalizedUserName = "USER1@LOCAL.TEST",
                Email = "user1@local.test",
                NormalizedEmail = "USER1@LOCAL.TEST",
            },
            new IdentityUser
            {
                Id = "user-2",
                UserName = "user2@local.test",
                NormalizedUserName = "USER2@LOCAL.TEST",
                Email = "user2@local.test",
                NormalizedEmail = "USER2@LOCAL.TEST",
            });
        await db.SaveChangesAsync();

        var checklist = new RecordingChecklistService();
        var notifications = new RecordingNotificationService();
        var service = CreateService(db, checklist, notifications);

        var tenderId = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender B",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Identified,
            AssignedUserIds = ["user-1", "user-2"],
        }, userId: "owner", isAdmin: false);

        Assert.Equal(2, notifications.AssignmentNotifications.Count);
        Assert.Contains(notifications.AssignmentNotifications, x => x.AssignedUserId == "user-1" && x.TenderId == tenderId);
        Assert.Contains(notifications.AssignmentNotifications, x => x.AssignedUserId == "user-2" && x.TenderId == tenderId);

        var statusNotification = Assert.Single(notifications.StatusChanges);
        Assert.Equal(TenderStatus.Draft, statusNotification.From);
        Assert.Equal(TenderStatus.Identified, statusNotification.To);
        Assert.Contains(tenderId, checklist.GeneratedTenderIds);
    }

    private static TenderService CreateService(
        ApplicationDbContext db,
        IChecklistService? checklistService = null,
        INotificationService? notificationService = null)
    {
        return new TenderService(
            db,
            checklistService ?? new NoOpChecklistService(),
            notificationService ?? new NoOpNotificationService(),
            new ReminderScheduler(db, TestDbFactory.CreateConfiguration()));
    }

    private sealed class NoOpChecklistService : IChecklistService
    {
        public Task GenerateChecklistAsync(Guid tenderId, string? templateName = null) => Task.CompletedTask;
        public Task<IEnumerable<ChecklistItem>> GetChecklistAsync(Guid tenderId, string userId) => Task.FromResult<IEnumerable<ChecklistItem>>(Array.Empty<ChecklistItem>());
        public Task<bool> AcquireLockAsync(int checklistItemId, string userId, TimeSpan? timeout = null) => Task.FromResult(false);
        public Task<bool> ReleaseLockAsync(int checklistItemId, string userId) => Task.FromResult(false);
        public Task MarkCompletedAsync(int checklistItemId, string userId) => Task.CompletedTask;
        public Task<ChecklistItem> AddItemAsync(Guid tenderId, Tenderizer.Dtos.CreateChecklistItemDto dto, string userId) => throw new NotSupportedException();
        public Task UpdateItemAsync(int checklistItemId, Tenderizer.Dtos.UpdateChecklistItemDto dto, string userId) => Task.CompletedTask;
        public Task RemoveItemAsync(int checklistItemId, string userId) => Task.CompletedTask;
    }

    private sealed class NoOpNotificationService : INotificationService
    {
        public Task NotifyTenderAssignedAsync(Guid tenderId, string tenderName, string assignedUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyTenderStatusChangedAsync(Guid tenderId, string tenderName, TenderStatus previousStatus, TenderStatus currentStatus, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingChecklistService : IChecklistService
    {
        public List<Guid> GeneratedTenderIds { get; } = [];

        public Task GenerateChecklistAsync(Guid tenderId, string? templateName = null)
        {
            GeneratedTenderIds.Add(tenderId);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ChecklistItem>> GetChecklistAsync(Guid tenderId, string userId) => Task.FromResult<IEnumerable<ChecklistItem>>(Array.Empty<ChecklistItem>());
        public Task<bool> AcquireLockAsync(int checklistItemId, string userId, TimeSpan? timeout = null) => Task.FromResult(false);
        public Task<bool> ReleaseLockAsync(int checklistItemId, string userId) => Task.FromResult(false);
        public Task MarkCompletedAsync(int checklistItemId, string userId) => Task.CompletedTask;
        public Task<ChecklistItem> AddItemAsync(Guid tenderId, Tenderizer.Dtos.CreateChecklistItemDto dto, string userId) => throw new NotSupportedException();
        public Task UpdateItemAsync(int checklistItemId, Tenderizer.Dtos.UpdateChecklistItemDto dto, string userId) => Task.CompletedTask;
        public Task RemoveItemAsync(int checklistItemId, string userId) => Task.CompletedTask;
    }

    private sealed class RecordingNotificationService : INotificationService
    {
        public List<(Guid TenderId, string TenderName, string AssignedUserId)> AssignmentNotifications { get; } = [];
        public List<(Guid TenderId, string TenderName, TenderStatus From, TenderStatus To)> StatusChanges { get; } = [];

        public Task NotifyTenderAssignedAsync(Guid tenderId, string tenderName, string assignedUserId, CancellationToken cancellationToken = default)
        {
            AssignmentNotifications.Add((tenderId, tenderName, assignedUserId));
            return Task.CompletedTask;
        }

        public Task NotifyTenderStatusChangedAsync(Guid tenderId, string tenderName, TenderStatus previousStatus, TenderStatus currentStatus, CancellationToken cancellationToken = default)
        {
            StatusChanges.Add((tenderId, tenderName, previousStatus, currentStatus));
            return Task.CompletedTask;
        }
    }
}
