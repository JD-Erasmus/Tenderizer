using Microsoft.EntityFrameworkCore;
using Tenderizer.Models;
using Tenderizer.Services.Implementations;

namespace TenderizerTest;

public sealed class ReminderSchedulerTests
{
    [Fact]
    public async Task ClearPendingAsync_DeletesOnlyUnsentReminders()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var config = TestDbFactory.CreateConfiguration();
        var scheduler = new ReminderScheduler(db, config);

        var tenderId = Guid.NewGuid();
        db.Tenders.Add(new Tender
        {
            Id = tenderId,
            Name = "T",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(10),
            Status = TenderStatus.Draft,
            OwnerUserId = "owner",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        db.TenderReminders.AddRange(
            new TenderReminder
            {
                Id = Guid.NewGuid(),
                TenderId = tenderId,
                ReminderAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                SentAtUtc = null,
                AttemptCount = 0,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            },
            new TenderReminder
            {
                Id = Guid.NewGuid(),
                TenderId = tenderId,
                ReminderAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                SentAtUtc = DateTimeOffset.UtcNow,
                AttemptCount = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            }
        );

        await db.SaveChangesAsync();

        await scheduler.ClearPendingAsync(tenderId);

        var reminders = await db.TenderReminders.AsNoTracking().Where(r => r.TenderId == tenderId).ToListAsync();
        Assert.Single(reminders);
        Assert.NotNull(reminders[0].SentAtUtc);
    }

    [Fact]
    public async Task RegenerateAsync_WhenTenderIsTerminal_ClearsPendingAndDoesNotCreateNew()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var config = TestDbFactory.CreateConfiguration();
        var scheduler = new ReminderScheduler(db, config);

        var tenderId = Guid.NewGuid();
        db.Tenders.Add(new Tender
        {
            Id = tenderId,
            Name = "T",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(10),
            Status = TenderStatus.Submitted,
            OwnerUserId = "owner",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        db.TenderReminders.Add(new TenderReminder
        {
            Id = Guid.NewGuid(),
            TenderId = tenderId,
            ReminderAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            SentAtUtc = null,
            AttemptCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();

        await scheduler.RegenerateAsync(tenderId);

        Assert.False(await db.TenderReminders.AsNoTracking().AnyAsync(r => r.TenderId == tenderId && r.SentAtUtc == null));
    }

    [Fact]
    public async Task RegenerateAsync_WhenActive_CreatesOnlyFutureReminders_DefaultSchedule()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var config = TestDbFactory.CreateConfiguration();
        var scheduler = new ReminderScheduler(db, config);

        var tenderId = Guid.NewGuid();
        var closing = DateTimeOffset.UtcNow.AddDays(5);

        db.Tenders.Add(new Tender
        {
            Id = tenderId,
            Name = "T",
            ClosingAtUtc = closing,
            Status = TenderStatus.InProgress,
            OwnerUserId = "owner",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();

        await scheduler.RegenerateAsync(tenderId);

        var reminders = await db.TenderReminders.AsNoTracking().Where(r => r.TenderId == tenderId).ToListAsync();

        // For closing in 5 days: T-7 is in the past (skipped), T-3 and T-24h are in the future.
        Assert.Equal(2, reminders.Count);

        var times = reminders.Select(r => r.ReminderAtUtc).OrderBy(x => x).ToArray();
        Assert.Contains(closing.AddDays(-3), times);
        Assert.Contains(closing.AddHours(-24), times);
        Assert.DoesNotContain(closing.AddDays(-7), times);
    }

    [Fact]
    public async Task RegenerateAsync_UsesReminderOffsetsMinutesOverride_AndSkipsPast()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var config = TestDbFactory.CreateConfiguration(new Dictionary<string, string?>
        {
            ["ReminderOffsetsMinutes:0"] = "120", // 2h
            ["ReminderOffsetsMinutes:1"] = "10",  // 10m
        });
        var scheduler = new ReminderScheduler(db, config);

        var tenderId = Guid.NewGuid();
        var closing = DateTimeOffset.UtcNow.AddMinutes(30);

        db.Tenders.Add(new Tender
        {
            Id = tenderId,
            Name = "T",
            ClosingAtUtc = closing,
            Status = TenderStatus.Draft,
            OwnerUserId = "owner",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();

        await scheduler.RegenerateAsync(tenderId);

        var times = await db.TenderReminders.AsNoTracking()
            .Where(r => r.TenderId == tenderId)
            .Select(r => r.ReminderAtUtc)
            .ToListAsync();

        // 2h offset is in the past (skipped); 10m offset is in the future.
        Assert.Single(times);
        Assert.Contains(closing.AddMinutes(-10), times);
    }

    [Fact]
    public async Task RegenerateAsync_IsIdempotent_WhenCalledTwice_DoesNotDuplicate()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var config = TestDbFactory.CreateConfiguration(new Dictionary<string, string?>
        {
            ["ReminderOffsetsMinutes:0"] = "60",
            ["ReminderOffsetsMinutes:1"] = "30",
        });
        var scheduler = new ReminderScheduler(db, config);

        var tenderId = Guid.NewGuid();
        var closing = DateTimeOffset.UtcNow.AddHours(2);

        db.Tenders.Add(new Tender
        {
            Id = tenderId,
            Name = "T",
            ClosingAtUtc = closing,
            Status = TenderStatus.Identified,
            OwnerUserId = "owner",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();

        await scheduler.RegenerateAsync(tenderId);
        await scheduler.RegenerateAsync(tenderId);

        var count = await db.TenderReminders.AsNoTracking().CountAsync(r => r.TenderId == tenderId);
        Assert.Equal(2, count);
    }
}
