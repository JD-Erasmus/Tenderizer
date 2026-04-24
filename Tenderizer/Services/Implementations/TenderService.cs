using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;
using Tenderizer.ViewModels;

namespace Tenderizer.Services.Implementations;

public sealed class TenderService : ITenderService
{
    private readonly ApplicationDbContext _db;
    private readonly IChecklistService _checklistService;
    private readonly INotificationService _notificationService;
    private readonly IReminderScheduler _reminderScheduler;

    private static readonly TenderStatus[] ActiveReminderStatuses =
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

    public TenderService(ApplicationDbContext db, IChecklistService checklistService, INotificationService notificationService, IReminderScheduler reminderScheduler)
    {
        _db = db;
        _checklistService = checklistService;
        _notificationService = notificationService;
        _reminderScheduler = reminderScheduler;
    }

    public async Task<IReadOnlyList<TenderListItemVm>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var historyCutoff = utcNow.AddDays(-90);

        var items = await ProjectTenderListItems()
            .Where(t =>
                (!TerminalStatuses.Contains(t.Status) && t.ClosingAtUtc >= utcNow.AddDays(-365))
                || (TerminalStatuses.Contains(t.Status) && t.UpdatedAtUtc >= historyCutoff))
            .ToListAsync(cancellationToken);

        return items
            .OrderBy(t => t.ClosingAtUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<TenderListItemVm>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var items = await ProjectTenderListItems()
            .ToListAsync(cancellationToken);

        return items
            .OrderBy(t => t.ClosingAtUtc)
            .ToList();
    }

    public async Task<TenderDetailsVm> GetDetailsAsync(Guid id, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var vm = await (
            from tender in _db.Tenders.AsNoTracking().Include(t => t.Assignments)
            join owner in _db.Users.AsNoTracking() on tender.OwnerUserId equals owner.Id into ownerGroup
            from owner in ownerGroup.DefaultIfEmpty()
            where tender.Id == id
            select new TenderDetailsVm
            {
                Id = tender.Id,
                Name = tender.Name,
                ReferenceNumber = tender.ReferenceNumber,
                Client = tender.Client,
                Category = tender.Category,
                ClosingAtUtc = tender.ClosingAtUtc,
                Status = tender.Status,
                OwnerUserId = tender.OwnerUserId,
                OwnerDisplayName = owner == null
                    ? tender.OwnerUserId
                    : owner.Email ?? owner.UserName ?? owner.Id,
                AssignedUserIds = tender.Assignments.Select(a => a.UserId).ToList(),
                CreatedAtUtc = tender.CreatedAtUtc,
                UpdatedAtUtc = tender.UpdatedAtUtc,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (vm is null)
        {
            throw new KeyNotFoundException("Tender not found.");
        }

        return vm;
    }

    public async Task<Guid> CreateAsync(TenderUpsertDto dto, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var ownerUserId = isAdmin ? dto.OwnerUserId : userId;
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("OwnerUserId is required.", nameof(dto));
        }

        ValidateClosingDate(dto.ClosingAtUtc, dto.Status, isAdmin);

        var utcNow = DateTimeOffset.UtcNow;

        var entity = new Tender
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            ReferenceNumber = dto.ReferenceNumber?.Trim(),
            Client = dto.Client?.Trim(),
            Category = dto.Category?.ToStorageValue(),
            ClosingAtUtc = dto.ClosingAtUtc,
            Status = dto.Status,
            OwnerUserId = ownerUserId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };

        _db.Tenders.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var assignedUserIds = await SyncAssignmentsAsync(entity.Id, dto.AssignedUserIds, cancellationToken);

        foreach (var assignedUserId in assignedUserIds)
        {
            await _notificationService.NotifyTenderAssignedAsync(entity.Id, entity.Name, assignedUserId, cancellationToken);
        }

        await GenerateChecklistIfNeededAsync(entity, previousStatus: null);

        // Keep reminders consistent with business rules.
        await _reminderScheduler.RegenerateAsync(entity.Id, cancellationToken);

        if (entity.Status != TenderStatus.Draft)
        {
            await _notificationService.NotifyTenderStatusChangedAsync(entity.Id, entity.Name, TenderStatus.Draft, entity.Status, cancellationToken);
        }

        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, TenderUpsertDto dto, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Tenders
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity is null)
        {
            throw new KeyNotFoundException("Tender not found.");
        }

        if (!isAdmin && !string.Equals(entity.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Only the owner or an admin can edit this tender.");
        }

        var oldOwnerUserId = entity.OwnerUserId;
        var oldClosing = entity.ClosingAtUtc;
        var oldStatus = entity.Status;

        var ownerUserId = isAdmin ? dto.OwnerUserId : entity.OwnerUserId;
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("OwnerUserId is required.", nameof(dto));
        }

        ValidateClosingDate(dto.ClosingAtUtc, dto.Status, isAdmin);

        entity.Name = dto.Name.Trim();
        entity.ReferenceNumber = dto.ReferenceNumber?.Trim();
        entity.Client = dto.Client?.Trim();
        entity.Category = dto.Category?.ToStorageValue();
        entity.ClosingAtUtc = dto.ClosingAtUtc;
        entity.Status = dto.Status;
        entity.OwnerUserId = ownerUserId;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var assignedUserIds = await SyncAssignmentsAsync(entity.Id, dto.AssignedUserIds, cancellationToken);

        foreach (var assignedUserId in assignedUserIds)
        {
            await _notificationService.NotifyTenderAssignedAsync(entity.Id, entity.Name, assignedUserId, cancellationToken);
        }

        await GenerateChecklistIfNeededAsync(entity, oldStatus);

        // Enforce reminder business rules:
        // - terminal => clear pending
        // - closing changed => regenerate (includes status rules)
        // - status changed => regenerate (includes terminal clear)
        await SyncRemindersAsync(entity.Id, entity.ClosingAtUtc, entity.Status, oldClosing, oldStatus, cancellationToken);

        if (!string.Equals(oldOwnerUserId, entity.OwnerUserId, StringComparison.Ordinal))
        {
            await _notificationService.NotifyTenderAssignedAsync(entity.Id, entity.Name, entity.OwnerUserId, cancellationToken);
        }

        if (entity.Status != oldStatus)
        {
            await _notificationService.NotifyTenderStatusChangedAsync(entity.Id, entity.Name, oldStatus, entity.Status, cancellationToken);
        }
    }

    public async Task UpdateStatusAsync(Guid id, TenderStatus status, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Tenders
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity is null)
        {
            throw new KeyNotFoundException("Tender not found.");
        }

        if (!isAdmin && !string.Equals(entity.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Only the owner or an admin can update tender status.");
        }

        if (entity.Status == status)
        {
            return;
        }

        ValidateClosingDate(entity.ClosingAtUtc, status, isAdmin);

        var oldStatus = entity.Status;
        entity.Status = status;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await GenerateChecklistIfNeededAsync(entity, oldStatus);
        await SyncRemindersAsync(entity.Id, entity.ClosingAtUtc, entity.Status, entity.ClosingAtUtc, oldStatus, cancellationToken);

        await _notificationService.NotifyTenderStatusChangedAsync(entity.Id, entity.Name, oldStatus, entity.Status, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Tenders
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity is null)
        {
            return;
        }

        _db.Tenders.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _reminderScheduler.ClearPendingAsync(id, cancellationToken);
    }

    private static void ValidateClosingDate(DateTimeOffset closingAtUtc, TenderStatus status, bool isAdmin)
    {
        if (!TerminalStatuses.Contains(status) && closingAtUtc <= DateTimeOffset.UtcNow)
        {
            if (!isAdmin)
            {
                throw new InvalidOperationException("Closing date/time must be in the future for non-terminal statuses.");
            }
        }

        if (isAdmin)
        {
            return;
        }

        if (TerminalStatuses.Contains(status))
        {
            return;
        }

        if (!ActiveReminderStatuses.Contains(status))
        {
            return;
        }
    }

    private IQueryable<TenderListItemVm> ProjectTenderListItems()
    {
        return
            from tender in _db.Tenders.AsNoTracking()
            join owner in _db.Users.AsNoTracking() on tender.OwnerUserId equals owner.Id into ownerGroup
            from owner in ownerGroup.DefaultIfEmpty()
            select new TenderListItemVm
            {
                Id = tender.Id,
                Name = tender.Name,
                ReferenceNumber = tender.ReferenceNumber,
                Client = tender.Client,
                Category = tender.Category,
                ClosingAtUtc = tender.ClosingAtUtc,
                Status = tender.Status,
                OwnerUserId = tender.OwnerUserId,
                OwnerDisplayName = owner == null
                    ? tender.OwnerUserId
                    : owner.Email ?? owner.UserName ?? owner.Id,
                UpdatedAtUtc = tender.UpdatedAtUtc,
            };
    }

    private async Task SyncRemindersAsync(
        Guid tenderId,
        DateTimeOffset newClosingAtUtc,
        TenderStatus newStatus,
        DateTimeOffset oldClosingAtUtc,
        TenderStatus oldStatus,
        CancellationToken cancellationToken)
    {
        if (TerminalStatuses.Contains(newStatus))
        {
            await _reminderScheduler.ClearPendingAsync(tenderId, cancellationToken);
        }
        else if (newClosingAtUtc != oldClosingAtUtc || newStatus != oldStatus)
        {
            await _reminderScheduler.RegenerateAsync(tenderId, cancellationToken);
        }
    }

    private async Task GenerateChecklistIfNeededAsync(Tender tender, TenderStatus? previousStatus)
    {
        if (tender.ChecklistGeneratedAt.HasValue)
        {
            return;
        }

        var shouldGenerate = previousStatus is null
            ? tender.Status == TenderStatus.Identified
            : previousStatus == TenderStatus.Draft && tender.Status == TenderStatus.Identified;

        if (!shouldGenerate)
        {
            return;
        }

        await _checklistService.GenerateChecklistAsync(tender.Id);
    }

    private async Task<IReadOnlyList<string>> SyncAssignmentsAsync(Guid tenderId, IEnumerable<string> assignedUserIds, CancellationToken cancellationToken)
    {
        var normalizedIds = assignedUserIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var validIds = await _db.Users
            .AsNoTracking()
            .Where(u => normalizedIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (validIds.Count != normalizedIds.Count)
        {
            throw new ArgumentException("One or more assigned users are invalid.");
        }

        var existingAssignments = await _db.TenderAssignments
            .Where(a => a.TenderId == tenderId)
            .ToListAsync(cancellationToken);

        _db.TenderAssignments.RemoveRange(existingAssignments);

        var utcNow = DateTimeOffset.UtcNow;
        foreach (var userId in validIds)
        {
            _db.TenderAssignments.Add(new TenderAssignment
            {
                TenderId = tenderId,
                UserId = userId,
                AssignedAt = utcNow,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return validIds;
    }
}
