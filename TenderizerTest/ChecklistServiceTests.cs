using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Implementations;
using Tenderizer.Services.Interfaces;

namespace TenderizerTest;

public sealed class ChecklistServiceTests
{
    [Fact]
    public async Task GenerateChecklistAsync_WhenTemplateExists_CreatesItemsAndSetsGeneratedAt()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var tender = await CreateTenderAsync(db, TenderStatus.Identified, ownerUserId: "owner-1");
        var service = CreateService(db, new[]
        {
            CreateTemplate("Default", ("RFP Document", true), ("Budget", true), ("CVs", false)),
        });

        await service.GenerateChecklistAsync(tender.Id);

        var reloaded = await db.Tenders.AsNoTracking().SingleAsync(x => x.Id == tender.Id);
        var items = await db.ChecklistItems.AsNoTracking().Where(x => x.TenderId == tender.Id).OrderBy(x => x.Id).ToListAsync();

        Assert.NotNull(reloaded.ChecklistGeneratedAt);
        Assert.Equal(3, items.Count);
        Assert.Collection(items,
            item =>
            {
                Assert.Equal("RFP Document", item.Title);
                Assert.True(item.Required);
            },
            item =>
            {
                Assert.Equal("Budget", item.Title);
                Assert.True(item.Required);
            },
            item =>
            {
                Assert.Equal("CVs", item.Title);
                Assert.False(item.Required);
            });
    }

    [Fact]
    public async Task MarkCompletedAsync_WhenChecklistItemExists_MarksCompleted()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var tender = await CreateTenderAsync(db, TenderStatus.InProgress, ownerUserId: "owner-1", assignedUserId: "user-1");
        var item = await CreateChecklistItemAsync(db, tender.Id, "Technical Specifications", required: true);
        var service = CreateService(db);

        await service.MarkCompletedAsync(item.Id, "user-1");

        var reloaded = await db.ChecklistItems.AsNoTracking().SingleAsync(x => x.Id == item.Id);
        Assert.True(reloaded.IsCompleted);
    }

    [Fact]
    public async Task AddUpdateRemoveItemAsync_WhenAssignedUserInIdentified_WorksEndToEnd()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var tender = await CreateTenderAsync(db, TenderStatus.Identified, ownerUserId: "owner-1", assignedUserId: "user-1");
        var service = CreateService(db);

        var created = await service.AddItemAsync(tender.Id, new CreateChecklistItemDto
        {
            Title = "Financial Statements",
            Description = "Last 3 years",
            Required = true,
        }, "user-1");

        await service.UpdateItemAsync(created.Id, new UpdateChecklistItemDto
        {
            Title = "Updated Financial Statements",
            Description = "Last 5 years",
            Required = false,
        }, "user-1");

        var updated = await db.ChecklistItems.AsNoTracking().SingleAsync(x => x.Id == created.Id);
        Assert.Equal("Updated Financial Statements", updated.Title);
        Assert.Equal("Last 5 years", updated.Description);
        Assert.False(updated.Required);

        await service.RemoveItemAsync(created.Id, "user-1");

        Assert.False(await db.ChecklistItems.AsNoTracking().AnyAsync(x => x.Id == created.Id));
    }

    private static ChecklistService CreateService(ApplicationDbContext db, IEnumerable<ChecklistTemplateConfig>? templates = null)
    {
        return new ChecklistService(db, new FakeTemplateProvider(templates ?? [CreateTemplate("Default", ("RFP Document", true))]));
    }

    private static async Task<Tender> CreateTenderAsync(ApplicationDbContext db, TenderStatus status, string ownerUserId, string? assignedUserId = null)
    {
        var tender = new Tender
        {
            Id = Guid.NewGuid(),
            Name = "Tender",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(5),
            Status = status,
            OwnerUserId = ownerUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.Tenders.Add(tender);

        if (!string.IsNullOrWhiteSpace(assignedUserId))
        {
            db.TenderAssignments.Add(new TenderAssignment
            {
                TenderId = tender.Id,
                UserId = assignedUserId,
                AssignedAt = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync();
        return tender;
    }

    private static async Task<ChecklistItem> CreateChecklistItemAsync(ApplicationDbContext db, Guid tenderId, string title, bool required)
    {
        var item = new ChecklistItem
        {
            TenderId = tenderId,
            Title = title,
            Required = required,
            IsCompleted = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static ChecklistTemplateConfig CreateTemplate(string name, params (string Title, bool Required)[] items)
    {
        return new ChecklistTemplateConfig
        {
            Name = name,
            Items = items.Select(x => new ChecklistTemplateItemConfig
            {
                Title = x.Title,
                Required = x.Required,
            }).ToList(),
        };
    }

    private sealed class FakeTemplateProvider : IChecklistTemplateProvider
    {
        private readonly IReadOnlyList<ChecklistTemplateConfig> _templates;

        public FakeTemplateProvider(IEnumerable<ChecklistTemplateConfig> templates)
        {
            _templates = templates.ToList();
        }

        public IEnumerable<ChecklistTemplateConfig> GetTemplates() => _templates;

        public ChecklistTemplateConfig? GetDefaultTemplate() => _templates.FirstOrDefault(x => string.Equals(x.Name, "Default", StringComparison.OrdinalIgnoreCase));
    }
}
