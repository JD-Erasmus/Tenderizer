using System.Text.Json;
using Tenderizer.Dtos;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public sealed class DocumentUploadRequestValidator : IDocumentUploadRequestValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".png", ".jpg", ".jpeg"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "text/csv",
        "image/png",
        "image/jpeg"
    };

    public IReadOnlyList<DocumentUploadValidationErrorDto> ValidateBaseline(DocumentUploadRequestDto request, IDocumentUploadRoute route)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(route);

        var errors = new List<DocumentUploadValidationErrorDto>();

        if (request.OwnerId == Guid.Empty)
        {
            errors.Add(new DocumentUploadValidationErrorDto
            {
                Field = nameof(request.OwnerId),
                Message = "Owner id is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.UploadedByUserId))
        {
            errors.Add(new DocumentUploadValidationErrorDto
            {
                Field = nameof(request.UploadedByUserId),
                Message = "UploadedByUserId is required."
            });
        }

        if (route.FileRequired && request.File is null)
        {
            errors.Add(new DocumentUploadValidationErrorDto
            {
                Field = nameof(request.File),
                Message = "File is required for this document type."
            });

            return errors;
        }

        if (request.File is null)
        {
            return errors;
        }

        if (request.File.Length <= 0)
        {
            errors.Add(new DocumentUploadValidationErrorDto
            {
                Field = nameof(request.File),
                Message = "File is empty."
            });
        }

        var fileName = request.File.FileName ?? string.Empty;
        var sanitizedFileName = Path.GetFileName(fileName);
        if (!string.Equals(fileName, sanitizedFileName, StringComparison.Ordinal))
        {
            errors.Add(new DocumentUploadValidationErrorDto
            {
                Field = nameof(request.File),
                Message = "Invalid file name."
            });
        }

        if (string.IsNullOrWhiteSpace(sanitizedFileName))
        {
            errors.Add(new DocumentUploadValidationErrorDto
            {
                Field = nameof(request.File),
                Message = "File name is required."
            });

            return errors;
        }

        var extension = Path.GetExtension(sanitizedFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            errors.Add(new DocumentUploadValidationErrorDto
            {
                Field = nameof(request.File),
                Message = "File extension is not allowed."
            });
        }

        if (!string.IsNullOrWhiteSpace(request.File.ContentType) && !AllowedContentTypes.Contains(request.File.ContentType))
        {
            errors.Add(new DocumentUploadValidationErrorDto
            {
                Field = nameof(request.File),
                Message = "File content type is not allowed."
            });
        }

        return errors;
    }

    public DocumentUploadMetadataBindingResultDto BindMetadata(DocumentUploadRequestDto request, IDocumentUploadRoute route)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(route);

        if (!route.MetadataRequired && string.IsNullOrWhiteSpace(request.MetadataJson))
        {
            return new DocumentUploadMetadataBindingResultDto
            {
                Success = true,
                Metadata = null,
            };
        }

        if (string.IsNullOrWhiteSpace(request.MetadataJson))
        {
            return Failed("MetadataJson", "Metadata is required.");
        }

        try
        {
            var metadata = JsonSerializer.Deserialize(request.MetadataJson, route.MetadataType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (metadata is null)
            {
                return Failed("MetadataJson", "Metadata payload could not be deserialized.");
            }

            return new DocumentUploadMetadataBindingResultDto
            {
                Success = true,
                Metadata = metadata,
            };
        }
        catch (JsonException)
        {
            return Failed("MetadataJson", "Metadata payload is invalid JSON or has invalid shape.");
        }
    }

    private static DocumentUploadMetadataBindingResultDto Failed(string field, string message)
    {
        return new DocumentUploadMetadataBindingResultDto
        {
            Success = false,
            ValidationErrors =
            [
                new DocumentUploadValidationErrorDto
                {
                    Field = field,
                    Message = message,
                }
            ]
        };
    }
}
