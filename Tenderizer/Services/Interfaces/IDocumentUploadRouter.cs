using Tenderizer.Models;

namespace Tenderizer.Services.Interfaces;

public interface IDocumentUploadRouter
{
    IDocumentUploadRoute Resolve(DocumentType documentType);
}
