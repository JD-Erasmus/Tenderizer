using System.Collections.Generic;
using Tenderizer.Models;

namespace Tenderizer.Services.Interfaces
{
    public interface IChecklistTemplateProvider
    {
        IEnumerable<ChecklistTemplateConfig> GetTemplates();
        ChecklistTemplateConfig? GetDefaultTemplate();
    }
}
