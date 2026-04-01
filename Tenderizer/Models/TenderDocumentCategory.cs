using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public enum TenderDocumentCategory
{
    [Display(Name = "Tender / RFP Document")]
    TenderRequestDocument = 0,

    [Display(Name = "Technical Proposal")]
    TechnicalProposal = 1,

    [Display(Name = "Checklist")]
    Checklist = 2,

    [Display(Name = "CV")]
    Cv = 3,

    [Display(Name = "Financial Proposal")]
    FinancialProposal = 4,

    [Display(Name = "Certificate")]
    Certificate = 5,

    [Display(Name = "Clarification")]
    Clarification = 6,

    [Display(Name = "Other")]
    Other = 7,
}
