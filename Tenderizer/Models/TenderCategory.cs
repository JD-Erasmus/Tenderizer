using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public enum TenderCategory
{
    [Display(Name = "Software")]
    Software = 0,

    [Display(Name = "Infrastructure")]
    Infrastructure = 1,

    [Display(Name = "Hardware Sales")]
    HardwareSales = 2,

    [Display(Name = "Professional Services")]
    ProfessionalServices = 3,

    [Display(Name = "Managed Services")]
    ManagedServices = 4,

    [Display(Name = "Cybersecurity")]
    Cybersecurity = 5,

    [Display(Name = "Office Equipment")]
    OfficeEquipment = 6,

    [Display(Name = "Other")]
    Other = 7,
}
