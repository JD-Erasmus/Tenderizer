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

    public TenderService(ApplicationDbContext db, IReminderScheduler reminderScheduler)
    {
        _db = db;
        _reminderScheduler = reminderScheduler;
    }

    public async Task<IReadOnlyList<TenderListItemVm>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var historyCutoff = utcNow.AddDays(-90);

        return await _db.Tenders
            .AsNoTracking()
            .Where(t =>
                (!TerminalStatuses.Contains(t.Status) && t.ClosingAtUtc >= utcNow.AddDays(-365))
                || (TerminalStatuses.Contains(t.Status) && t.UpdatedAtUtc >= historyCutoff))
            .OrderBy(t => t.ClosingAtUtc)
            .Select(t => new TenderListItemVm
            {
                Id = t.Id,
                Name = t.Name,
                Client = t.Client,
                ClosingAtUtc = t.ClosingAtUtc,
                Status = t.Status,
                OwnerUserId = t.OwnerUserId,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenderListItemVm>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Tenders
            .AsNoTracking()
            .OrderBy(t => t.ClosingAtUtc)
            .Select(t => new TenderListItemVm
            {
                Id = t.Id,
                Name = t.Name,
                Client = t.Client,
                ClosingAtUtc = t.ClosingAtUtc,
                Status = t.Status,
                OwnerUserId = t.OwnerUserId,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TenderDetailsVm> GetDetailsAsync(Guid id, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var vm = await _db.Tenders
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TenderDetailsVm
            {
                Id = t.Id,
                Name = t.Name,
                ReferenceNumber = t.ReferenceNumber,
                Client = t.Client,
                Category = t.Category,
                ClosingAtUtc = t.ClosingAtUtc,
                Status = t.Status,
                OwnerUserId = t.OwnerUserId,
                CreatedAtUtc = t.CreatedAtUtc,
                UpdatedAtUtc = t.UpdatedAtUtc,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (vm is null)
        {
            throw new KeyNotFoundException("Tender not found.");
        }

        if (!isAdmin && !string.Equals(vm.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Only the owner or an admin can view this tender.");
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
            Category = dto.Category?.Trim(),
            ClosingAtUtc = dto.ClosingAtUtc,
            Status = dto.Status,
            OwnerUserId = ownerUserId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };

        _db.Tenders.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        // Keep reminders consistent with business rules.
        await _reminderScheduler.RegenerateAsync(entity.Id, cancellationToken);

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
        entity.Category = dto.Category?.Trim();
        entity.ClosingAtUtc = dto.ClosingAtUtc;
        entity.Status = dto.Status;
        entity.OwnerUserId = ownerUserId;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        // Enforce reminder business rules:
        // - terminal => clear pending
        // - closing changed => regenerate (includes status rules)
        // - status changed => regenerate (includes terminal clear)
        if (TerminalStatuses.Contains(entity.Status))
        {
            await _reminderScheduler.ClearPendingAsync(entity.Id, cancellationToken);
        }
        else if (entity.ClosingAtUtc != oldClosing || entity.Status != oldStatus)
        {
            await _reminderScheduler.RegenerateAsync(entity.Id, cancellationToken);
        }
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
}
