using Tenderizer.Dtos;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public sealed class DocumentUploadService : IDocumentUploadService
{
    private readonly IDocumentUploadRouter _router;
    private readonly IDocumentUploadRequestValidator _requestValidator;

    public DocumentUploadService(IDocumentUploadRouter router, IDocumentUploadRequestValidator requestValidator)
    {
        _router = router;
        _requestValidator = requestValidator;
    }

    public async Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, bool isAdmin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        IDocumentUploadRoute route;
        try
        {
            route = _router.Resolve(request.DocumentType);
        }
        catch (KeyNotFoundException)
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "route_not_found",
                ErrorMessage = $"No upload route is registered for '{request.DocumentType}'.",
                ValidationErrors =
                [
                    new DocumentUploadValidationErrorDto
                    {
                        Field = nameof(request.DocumentType),
                        Message = $"No upload route is registered for '{request.DocumentType}'.",
                    }
                ]
            };
        }

        var baselineValidationErrors = _requestValidator.ValidateBaseline(request, route);
        if (baselineValidationErrors.Count > 0)
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "validation_failed",
                ErrorMessage = "Upload request validation failed.",
                ValidationErrors = baselineValidationErrors,
            };
        }

        var metadataBinding = _requestValidator.BindMetadata(request, route);
        if (!metadataBinding.Success)
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "invalid_metadata",
                ErrorMessage = "Metadata validation failed.",
                ValidationErrors = metadataBinding.ValidationErrors,
            };
        }

        var metadata = metadataBinding.Metadata;
        if (metadata is null && route.MetadataRequired)
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "invalid_metadata",
                ErrorMessage = "Metadata payload is required.",
                ValidationErrors =
                [
                    new DocumentUploadValidationErrorDto
                    {
                        Field = nameof(request.MetadataJson),
                        Message = "Metadata payload is required.",
                    }
                ]
            };
        }

        if (metadata is null)
        {
            return await route.UploadAsync(request, new object(), isAdmin, cancellationToken);
        }

        return await route.UploadAsync(request, metadata, isAdmin, cancellationToken);
    }
}
