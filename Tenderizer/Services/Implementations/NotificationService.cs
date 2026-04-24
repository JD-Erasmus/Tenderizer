using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;

    public NotificationService(ApplicationDbContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public async Task NotifyTenderAssignedAsync(Guid tenderId, string tenderName, string assignedUserId, CancellationToken cancellationToken = default)
    {
        var recipient = await _db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assignedUserId, cancellationToken);
        if (!TryGetRecipientEmail(recipient, out var email))
        {
            return;
        }

        var subject = $"Tender assigned: {tenderName}";
        var body = BuildAssignmentBody(tenderId, tenderName, recipient);
        await SendWithRetryAsync(email, subject, body, cancellationToken);
    }

    public async Task NotifyTenderStatusChangedAsync(Guid tenderId, string tenderName, TenderStatus previousStatus, TenderStatus currentStatus, CancellationToken cancellationToken = default)
    {
        var tender = await _db.Tenders
            .AsNoTracking()
            .Include(x => x.Assignments)
            .SingleOrDefaultAsync(x => x.Id == tenderId, cancellationToken);

        if (tender is null)
        {
            return;
        }

        var recipientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            tender.OwnerUserId,
        };

        foreach (var assignment in tender.Assignments)
        {
            recipientIds.Add(assignment.UserId);
        }

        var recipients = await _db.Users
            .AsNoTracking()
            .Where(x => recipientIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (recipients.Count == 0)
        {
            return;
        }

        var subject = $"Tender status changed: {tenderName}";
        var body = BuildStatusChangedBody(tender, tenderName, previousStatus, currentStatus, recipients);

        foreach (var recipient in recipients)
        {
            if (!TryGetRecipientEmail(recipient, out var email))
            {
                continue;
            }

            await SendWithRetryAsync(email, subject, body, cancellationToken);
        }
    }

    private static bool TryGetRecipientEmail(IdentityUser? user, out string email)
    {
        email = string.Empty;
        if (user is null)
        {
            return false;
        }

        email = user.Email ?? string.Empty;
        return !string.IsNullOrWhiteSpace(email);
    }

    private static string BuildAssignmentBody(Guid tenderId, string tenderName, IdentityUser recipient)
    {
        var title = WebUtility.HtmlEncode(tenderName);
        var name = WebUtility.HtmlEncode(recipient.Email ?? recipient.UserName ?? recipient.Id);
        var link = WebUtility.HtmlEncode($"/tenders/{tenderId:D}");

        return $"<p>Hello {name},</p><p>You have been assigned to tender <strong>{title}</strong>.</p><p><a href=\"{link}\">View tender</a></p>";
    }

    private static string BuildStatusChangedBody(Tender tender, string tenderName, TenderStatus previousStatus, TenderStatus currentStatus, IReadOnlyCollection<IdentityUser> recipients)
    {
        var title = WebUtility.HtmlEncode(tenderName);
        var statusFrom = WebUtility.HtmlEncode(previousStatus.ToString());
        var statusTo = WebUtility.HtmlEncode(currentStatus.ToString());
        var link = WebUtility.HtmlEncode($"/tenders/{tender.Id:D}");

        var assignedUsers = recipients
            .Select(x => WebUtility.HtmlEncode(x.Email ?? x.UserName ?? x.Id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var assignedList = assignedUsers.Count == 0
            ? "<li>No assigned users</li>"
            : string.Join(string.Empty, assignedUsers.Select(x => $"<li>{x}</li>"));

        return $"<p>The tender <strong>{title}</strong> changed from <strong>{statusFrom}</strong> to <strong>{statusTo}</strong>.</p><p>Assigned users:</p><ul>{assignedList}</ul><p><a href=\"{link}\">View tender</a></p>";
    }

    private async Task SendWithRetryAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _emailSender.SendAsync(to, subject, htmlBody, cancellationToken);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                if (attempt == maxAttempts)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
            }
        }
    }
}
