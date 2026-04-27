using Tenderizer.Models;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public sealed class DocumentUploadRouter : IDocumentUploadRouter
{
    private readonly IReadOnlyDictionary<DocumentType, IDocumentUploadRoute> _routes;

    public DocumentUploadRouter(IEnumerable<IDocumentUploadRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var routeDictionary = new Dictionary<DocumentType, IDocumentUploadRoute>();

        foreach (var route in routes)
        {
            if (!routeDictionary.TryAdd(route.DocumentType, route))
            {
                throw new InvalidOperationException($"Multiple upload routes are registered for '{route.DocumentType}'.");
            }
        }

        _routes = routeDictionary;
    }

    public IDocumentUploadRoute Resolve(DocumentType documentType)
    {
        if (_routes.TryGetValue(documentType, out var route))
        {
            return route;
        }

        throw new KeyNotFoundException($"No upload route is registered for '{documentType}'.");
    }
}
