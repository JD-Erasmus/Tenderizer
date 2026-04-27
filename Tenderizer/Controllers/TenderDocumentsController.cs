using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;
using Tenderizer.ViewModels;

namespace Tenderizer.Controllers;

[Authorize]
[Route("tenders/{tenderId:guid}/documents")]
public sealed class TenderDocumentsController : Controller
{
    private readonly ITenderDocumentService _tenderDocumentService;
    private readonly IDocumentUploadService _documentUploadService;
    private readonly IChecklistService _checklistService;
    private readonly IPrivateFileStore _privateFileStore;

    public TenderDocumentsController(
        ITenderDocumentService tenderDocumentService,
        IDocumentUploadService documentUploadService,
        IChecklistService checklistService,
        IPrivateFileStore privateFileStore)
    {
        _tenderDocumentService = tenderDocumentService;
        _documentUploadService = documentUploadService;
        _checklistService = checklistService;
        _privateFileStore = privateFileStore;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid tenderId, int? checklistItemId, CancellationToken cancellationToken)
    {
        try
        {
            var vm = await _tenderDocumentService.GetIndexAsync(
                tenderId,
                GetUserId(),
                User.IsInRole("Admin"),
                cancellationToken);

            vm.Upload.ChecklistItemId = checklistItemId;

            try
            {
                vm.ChecklistItems = (await _checklistService.GetChecklistAsync(tenderId, GetUserId()))
                    .Select(item => MapChecklistItem(item))
                    .ToList();
                vm.CanViewChecklist = true;
            }
            catch (UnauthorizedAccessException)
            {
                vm.ChecklistItems = Array.Empty<ChecklistItemVm>();
                vm.CanViewChecklist = false;
            }

            return View(vm);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(Guid tenderId, [Bind(Prefix = "Upload")] TenderDocumentUploadVm vm, CancellationToken cancellationToken)
    {
        if (vm.File is null)
        {
            TempData["ErrorMessage"] = "Select a file to upload.";
            return RedirectToAction(nameof(Index), new { tenderId });
        }

        try
        {
            var metadata = new TenderDocumentUploadMetadata
            {
                ChecklistItemId = vm.ChecklistItemId,
                Category = vm.Category,
                DisplayName = vm.DisplayName,
            };

            var result = await _documentUploadService.UploadAsync(new DocumentUploadRequestDto
            {
                DocumentType = DocumentType.TenderDocument,
                OwnerId = tenderId,
                UploadedByUserId = GetUserId(),
                File = vm.File,
                MetadataJson = JsonSerializer.Serialize(metadata),
            }, User.IsInRole("Admin"), cancellationToken);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Tender document upload failed.";
                return RedirectToAction(nameof(Index), new { tenderId });
            }

            TempData["StatusMessage"] = "Tender document uploaded.";
            return RedirectToAction(nameof(Index), new { tenderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index), new { tenderId });
        }
    }

    [HttpPost("attach-library")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AttachLibrary(Guid tenderId, [Bind(Prefix = "AttachLibrary")] TenderDocumentAttachLibraryVm vm, CancellationToken cancellationToken)
    {
        if (!vm.LibraryDocumentVersionId.HasValue)
        {
            TempData["ErrorMessage"] = "Select a library document version to attach.";
            return RedirectToAction(nameof(Index), new { tenderId });
        }

        try
        {
            await _tenderDocumentService.AttachLibraryVersionAsync(tenderId, new TenderDocumentAttachLibraryDto
            {
                LibraryDocumentVersionId = vm.LibraryDocumentVersionId.Value,
                Category = vm.Category,
                DisplayName = vm.DisplayName,
            }, GetUserId(), User.IsInRole("Admin"), cancellationToken);

            TempData["StatusMessage"] = "Library document attached to tender.";
            return RedirectToAction(nameof(Index), new { tenderId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index), new { tenderId });
        }
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid tenderId, Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            var descriptor = await _tenderDocumentService.GetDownloadAsync(
                tenderId,
                documentId,
                GetUserId(),
                User.IsInRole("Admin"),
                cancellationToken);

            var file = await _privateFileStore.OpenReadAsync(descriptor.StoredFile, descriptor.DownloadFileName, cancellationToken);
            return File(file.Stream, file.ContentType, file.DownloadFileName, enableRangeProcessing: true);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    private static ChecklistItemVm MapChecklistItem(ChecklistItem item)
    {
        return new ChecklistItemVm
        {
            Id = item.Id,
            TenderId = item.TenderId,
            Title = item.Title,
            Description = item.Description,
            Required = item.Required,
            IsCompleted = item.IsCompleted,
            LockedByUserId = item.LockedByUserId,
            LockedAtUtc = item.LockedAtUtc,
            LockExpiresAtUtc = item.LockExpiresAtUtc,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,
        };
    }
}
