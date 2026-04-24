using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Dtos
{
    public class CreateChecklistItemDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool Required { get; set; }
    }
}
