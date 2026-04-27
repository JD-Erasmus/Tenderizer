using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public class ChecklistService : IChecklistService
{
    private readonly ApplicationDbContext _db;
    private readonly IChecklistTemplateProvider _templateProvider;

    public ChecklistService(ApplicationDbContext db, IChecklistTemplateProvider templateProvider)
    {
        _db = db;
        _templateProvider = templateProvider;
    }

    public async Task GenerateChecklistAsync(Guid tenderId, string? templateName = null)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var tender = await _db.Tenders
            .Include(x => x.ChecklistItems)
            .SingleOrDefaultAsync(x => x.Id == tenderId);

        if (tender == null)
        {
            throw new KeyNotFoundException("Tender not found");
        }

        if (tender.ChecklistGeneratedAt.HasValue || tender.ChecklistItems.Count > 0)
        {
            if (!tender.ChecklistGeneratedAt.HasValue)
            {
                tender.ChecklistGeneratedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return;
        }

        var template = templateName == null ? _templateProvider.GetDefaultTemplate() : _templateProvider.GetTemplates().FirstOrDefault(t => t.Name == templateName);
        if (template == null) return;

        var items = template.Items.Select(i => new ChecklistItem
        {
            TenderId = tenderId,
            Title = i.Title,
            Description = null,
            Required = i.Required,
            IsCompleted = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        }).ToList();

        _db.ChecklistItems.AddRange(items);
        tender.ChecklistGeneratedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<IEnumerable<ChecklistItem>> GetChecklistAsync(Guid tenderId, string userId)
    {
        var tender = await LoadTenderAsync(tenderId)
            ?? throw new KeyNotFoundException("Tender not found");

        await EnsureChecklistAccessAsync(tender, userId);

        return await _db.ChecklistItems
            .AsNoTracking()
            .Where(x => x.TenderId == tenderId)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    public async Task MarkCompletedAsync(int checklistItemId, string userId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var item = await LoadChecklistItemAsync(checklistItemId)
            ?? throw new KeyNotFoundException();

        await EnsureChecklistAccessAsync(item.Tender, userId);

        if (item.IsCompleted)
        {
            return;
        }

        item.IsCompleted = true;
        item.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<ChecklistItem> AddItemAsync(Guid tenderId, CreateChecklistItemDto dto, string userId)
    {
        var tender = await LoadTenderAsync(tenderId)
            ?? throw new KeyNotFoundException("Tender not found");

        await EnsureChecklistAccessAsync(tender, userId);

        var item = new ChecklistItem
        {
            TenderId = tenderId,
            Title = dto.Title,
            Description = dto.Description,
            Required = dto.Required,
            IsCompleted = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _db.ChecklistItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task UpdateItemAsync(int checklistItemId, UpdateChecklistItemDto dto, string userId)
    {
        var item = await LoadChecklistItemAsync(checklistItemId)
            ?? throw new KeyNotFoundException();

        await EnsureChecklistAccessAsync(item.Tender, userId);

        item.Title = dto.Title;
        item.Description = dto.Description;
        item.Required = dto.Required;
        item.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(int checklistItemId, string userId)
    {
        var item = await LoadChecklistItemAsync(checklistItemId)
            ?? throw new KeyNotFoundException();

        await EnsureChecklistAccessAsync(item.Tender, userId);

        _db.ChecklistItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    private async Task<Tender?> LoadTenderAsync(Guid tenderId)
    {
        return await _db.Tenders
            .Include(x => x.Assignments)
            .SingleOrDefaultAsync(x => x.Id == tenderId);
    }

    private async Task<ChecklistItem?> LoadChecklistItemAsync(int checklistItemId)
    {
        return await _db.ChecklistItems
            .Include(x => x.Tender)
            .ThenInclude(x => x.Assignments)
            .SingleOrDefaultAsync(x => x.Id == checklistItemId);
    }

    private async Task EnsureChecklistAccessAsync(Tender tender, string userId)
    {
        if (await IsAdminAsync(userId))
        {
            return;
        }

        if (string.Equals(tender.OwnerUserId, userId, StringComparison.Ordinal))
        {
            return;
        }

        if ((tender.Status is TenderStatus.Identified or TenderStatus.InProgress) &&
            tender.Assignments.Any(x => string.Equals(x.UserId, userId, StringComparison.Ordinal)))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only the owner, an assigned user, or an admin can manage checklist items.");
    }

    private async Task<bool> IsAdminAsync(string userId)
    {
        return await (from userRole in _db.UserRoles
                      join role in _db.Roles on userRole.RoleId equals role.Id
                      where userRole.UserId == userId && role.Name == "Admin"
                      select role.Id).AnyAsync();
    }
}
