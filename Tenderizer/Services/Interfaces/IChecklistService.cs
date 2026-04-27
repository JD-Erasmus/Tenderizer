using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tenderizer.Dtos;
using Tenderizer.Models;

namespace Tenderizer.Services.Interfaces
{
    public interface IChecklistService
    {
        Task GenerateChecklistAsync(Guid tenderId, string? templateName = null);
        Task<IEnumerable<ChecklistItem>> GetChecklistAsync(Guid tenderId, string userId);
        Task MarkCompletedAsync(int checklistItemId, string userId);
        Task<ChecklistItem> AddItemAsync(Guid tenderId, CreateChecklistItemDto dto, string userId);
        Task UpdateItemAsync(int checklistItemId, UpdateChecklistItemDto dto, string userId);
        Task RemoveItemAsync(int checklistItemId, string userId);
    }
}
