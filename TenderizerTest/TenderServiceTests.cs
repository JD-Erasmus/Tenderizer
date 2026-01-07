using Microsoft.EntityFrameworkCore;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Implementations;

namespace TenderizerTest;

public sealed class TenderServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenNonAdmin_UsesCurrentUserAsOwner_AndSetsAuditFields()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var scheduler = new ReminderScheduler(db, TestDbFactory.CreateConfiguration());
        var service = new TenderService(db, scheduler);

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

        var scheduler = new ReminderScheduler(db, TestDbFactory.CreateConfiguration());
        var service = new TenderService(db, scheduler);

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

        var scheduler = new ReminderScheduler(db, TestDbFactory.CreateConfiguration());
        var service = new TenderService(db, scheduler);

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

        var scheduler = new ReminderScheduler(db, TestDbFactory.CreateConfiguration());
        var service = new TenderService(db, scheduler);

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

        var scheduler = new ReminderScheduler(db, TestDbFactory.CreateConfiguration());
        var service = new TenderService(db, scheduler);

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

        var scheduler = new ReminderScheduler(db, TestDbFactory.CreateConfiguration());
        var service = new TenderService(db, scheduler);

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
            Category = " cat ",
        };

        await service.UpdateAsync(id, dto, userId: "owner", isAdmin: false);

        var after = await db.Tenders.AsNoTracking().SingleAsync(t => t.Id == id);
        Assert.Equal("Tender A2", after.Name);
        Assert.Equal("r2", after.ReferenceNumber);
        Assert.Equal("c", after.Client);
        Assert.Equal("cat", after.Category);
        Assert.Equal(TenderStatus.InProgress, after.Status);
        Assert.Equal("owner", after.OwnerUserId);
        Assert.True(after.UpdatedAtUtc > before.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenNonOwnerNonAdmin_ThrowsUnauthorized()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var scheduler = new ReminderScheduler(db, TestDbFactory.CreateConfiguration());
        var service = new TenderService(db, scheduler);

        var id = await service.CreateAsync(new TenderUpsertDto
        {
            Name = "Tender A",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = TenderStatus.Draft,
        }, userId: "owner", isAdmin: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDetailsAsync(id, userId: "other", isAdmin: false));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTender()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var scheduler = new ReminderScheduler(db, TestDbFactory.CreateConfiguration());
        var service = new TenderService(db, scheduler);

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
}
