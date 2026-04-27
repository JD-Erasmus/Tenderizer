using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public enum LibraryDocumentType
{
    [Display(Name = "CV")]
    Cv = 0,

    [Display(Name = "Certificate")]
    Certificate = 1,

    [Display(Name = "Policy")]
    Policy = 2,

    [Display(Name = "Template")]
    Template = 3,

    [Display(Name = "Other")]
    Other = 4,
}
