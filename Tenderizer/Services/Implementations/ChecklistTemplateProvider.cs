using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations
{
    public class ChecklistTemplateProvider : IChecklistTemplateProvider
    {
        private readonly IEnumerable<ChecklistTemplateConfig> _templates;

        public ChecklistTemplateProvider(IConfiguration configuration)
        {
            _templates = configuration.GetSection("ChecklistTemplates").Get<List<ChecklistTemplateConfig>>() ?? Enumerable.Empty<ChecklistTemplateConfig>();
        }

        public IEnumerable<ChecklistTemplateConfig> GetTemplates() => _templates;

        public ChecklistTemplateConfig? GetDefaultTemplate() => _templates.FirstOrDefault(t => t.Name == "Default") ?? _templates.FirstOrDefault();
    }
}
