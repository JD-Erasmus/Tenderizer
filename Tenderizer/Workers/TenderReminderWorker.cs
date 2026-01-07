using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Workers;

public sealed class TenderReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenderReminderWorker> _logger;

    private static readonly TenderStatus[] TerminalStatuses =
    [
        TenderStatus.Submitted,
        TenderStatus.Won,
        TenderStatus.Lost,
        TenderStatus.Cancelled,
    ];

    public TenderReminderWorker(IServiceScopeFactory scopeFactory, ILogger<TenderReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unhandled exception in TenderReminderWorker loop.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var nowUtc = DateTimeOffset.UtcNow;

        var dueReminders = await db.TenderReminders
            .AsNoTracking()
            .Where(r =>
                r.SentAtUtc == null
                && r.ReminderAtUtc <= nowUtc
                && !TerminalStatuses.Contains(r.Tender.Status))
            .Select(r => new
            {
                r.Id,
                r.TenderId,
                r.ReminderAtUtc,
                r.AttemptCount,
                Tender = new { r.Tender.Id, r.Tender.Name, r.Tender.ClosingAtUtc, r.Tender.Status, r.Tender.OwnerUserId }
            })
            .ToListAsync(cancellationToken);

        foreach (var reminder in dueReminders)
        {
            if (reminder.AttemptCount >= 5)
            {
                continue;
            }

            var user = await userManager.FindByIdAsync(reminder.Tender.OwnerUserId);
            var recipient = user?.Email;
            if (string.IsNullOrWhiteSpace(recipient))
            {
                await MarkFailureAsync(db, reminder.Id, "Owner user email not found.", reschedule: true, cancellationToken);
                continue;
            }

            try
            {
                var subject = $"Tender reminder: {reminder.Tender.Name} closes {reminder.Tender.ClosingAtUtc:yyyy-MM-dd HH:mm} UTC";
                var body = BuildHtml(reminder.Tender.Name, reminder.Tender.ClosingAtUtc, reminder.Tender.Status);

                await emailSender.SendAsync(recipient, subject, body, cancellationToken);

                await MarkSentAsync(db, reminder.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                await MarkFailureAsync(db, reminder.Id, ex.Message, reschedule: true, cancellationToken);
            }
        }

        // Cleanup: if a tender becomes terminal, delete any pending reminders that might exist.
        await db.TenderReminders
            .Where(r => r.SentAtUtc == null && TerminalStatuses.Contains(r.Tender.Status))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string BuildHtml(string tenderName, DateTimeOffset closingAtUtc, TenderStatus status)
    {
        return $"""
<!doctype html>
<html>
  <body>
    <h2>Tender Reminder</h2>
    <p><strong>{System.Net.WebUtility.HtmlEncode(tenderName)}</strong></p>
    <ul>
      <li>Closing (UTC): {closingAtUtc:yyyy-MM-dd HH:mm} UTC</li>
      <li>Status: {status}</li>
    </ul>
  </body>
</html>
""";
    }

    private static async Task MarkSentAsync(ApplicationDbContext db, Guid reminderId, CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var entity = await db.TenderReminders.SingleOrDefaultAsync(r => r.Id == reminderId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        if (entity.SentAtUtc is not null)
        {
            return;
        }

        entity.SentAtUtc = utcNow;
        entity.LastError = null;
        await db.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }

    private static async Task MarkFailureAsync(ApplicationDbContext db, Guid reminderId, string error, bool reschedule, CancellationToken cancellationToken)
    {
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var entity = await db.TenderReminders.SingleOrDefaultAsync(r => r.Id == reminderId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        if (entity.SentAtUtc is not null)
        {
            return;
        }

        entity.AttemptCount += 1;
        entity.LastError = error.Length > 500 ? error[..500] : error;

        if (reschedule)
        {
            entity.ReminderAtUtc = DateTimeOffset.UtcNow.AddMinutes(10);
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}
