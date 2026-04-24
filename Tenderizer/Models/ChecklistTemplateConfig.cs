using System.Collections.Generic;

namespace Tenderizer.Models
{
    public class ChecklistTemplateConfig
    {
        public string Name { get; set; } = string.Empty;
        public List<ChecklistTemplateItemConfig> Items { get; set; } = new();
    }

    public class ChecklistTemplateItemConfig
    {
        public string Title { get; set; } = string.Empty;
        public bool Required { get; set; }
    }
}
