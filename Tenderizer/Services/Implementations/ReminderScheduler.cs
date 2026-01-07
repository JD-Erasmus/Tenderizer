using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public sealed class ReminderScheduler : IReminderScheduler
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    private static readonly TenderStatus[] ActiveStatuses =
    [
        TenderStatus.Draft,
        TenderStatus.Identified,
        TenderStatus.InProgress,
    ];

    private static readonly TenderStatus[] TerminalStatuses =
    [
        TenderStatus.Submitted,
        TenderStatus.Won,
        TenderStatus.Lost,
        TenderStatus.Cancelled,
    ];

    public ReminderScheduler(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task RegenerateAsync(Guid tenderId, CancellationToken cancellationToken = default)
    {
        var tender = await _db.Tenders
            .AsNoTracking()
            .Where(t => t.Id == tenderId)
            .Select(t => new { t.Id, t.ClosingAtUtc, t.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (tender is null)
        {
            throw new KeyNotFoundException("Tender not found.");
        }

        // Always clear pending reminders first; if status is terminal we stop here.
        await ClearPendingAsync(tenderId, cancellationToken);

        if (TerminalStatuses.Contains(tender.Status))
        {
            return;
        }

        if (!ActiveStatuses.Contains(tender.Status))
        {
            return;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var offsets = GetOffsets();

        // Option B: skip reminders in the past.
        var desiredTimes = offsets
            .Select(offset => tender.ClosingAtUtc.Add(-offset))
            .Where(reminderAtUtc => reminderAtUtc > nowUtc)
            .Distinct()
            .ToArray();

        if (desiredTimes.Length == 0)
        {
            return;
        }

        // If concurrent calls happen, there may already be pending reminders inserted after our delete.
        var existingTimes = await _db.TenderReminders
            .AsNoTracking()
            .Where(r => r.TenderId == tenderId && r.SentAtUtc == null)
            .Select(r => r.ReminderAtUtc)
            .ToListAsync(cancellationToken);

        var toInsert = desiredTimes
            .Where(t => !existingTimes.Contains(t))
            .ToArray();

        if (toInsert.Length == 0)
        {
            return;
        }

        var createdAtUtc = DateTimeOffset.UtcNow;
        foreach (var reminderAtUtc in toInsert)
        {
            _db.TenderReminders.Add(new TenderReminder
            {
                Id = Guid.NewGuid(),
                TenderId = tenderId,
                ReminderAtUtc = reminderAtUtc,
                SentAtUtc = null,
                AttemptCount = 0,
                LastError = null,
                CreatedAtUtc = createdAtUtc,
            });
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique constraint on (TenderId, ReminderAtUtc) may trip in rare races.
            // Treat as idempotent: callers can retry and the final DB state is correct.
            _db.ChangeTracker.Clear();
        }
    }

    public async Task ClearPendingAsync(Guid tenderId, CancellationToken cancellationToken = default)
    {
        await _db.TenderReminders
            .Where(r => r.TenderId == tenderId && r.SentAtUtc == null)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private IReadOnlyList<TimeSpan> GetOffsets()
    {
        var minutes = _configuration.GetSection("ReminderOffsetsMinutes").Get<int[]>();
        if (minutes is { Length: > 0 })
        {
            return minutes
                .Where(m => m > 0)
                .Select(m => TimeSpan.FromMinutes(m))
                .ToArray();
        }

        return
        [
            TimeSpan.FromDays(7),
            TimeSpan.FromDays(3),
            TimeSpan.FromHours(24),
        ];
    }
}
