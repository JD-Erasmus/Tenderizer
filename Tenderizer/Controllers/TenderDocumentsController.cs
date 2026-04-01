using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tenderizer.Dtos;
using Tenderizer.Services.Interfaces;
using Tenderizer.ViewModels;

namespace Tenderizer.Controllers;

[Authorize]
[Route("tenders/{tenderId:guid}/documents")]
public sealed class TenderDocumentsController : Controller
{
    private readonly ITenderDocumentService _tenderDocumentService;
    private readonly IPrivateFileStore _privateFileStore;

    public TenderDocumentsController(
        ITenderDocumentService tenderDocumentService,
        IPrivateFileStore privateFileStore)
    {
        _tenderDocumentService = tenderDocumentService;
        _privateFileStore = privateFileStore;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid tenderId, CancellationToken cancellationToken)
    {
        try
        {
            var vm = await _tenderDocumentService.GetIndexAsync(
                tenderId,
                GetUserId(),
                User.IsInRole("Admin"),
                cancellationToken);

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
            await _tenderDocumentService.UploadAsync(tenderId, new TenderDocumentUploadDto
            {
                Category = vm.Category,
                DisplayName = vm.DisplayName,
                File = vm.File,
                PersonName = vm.PersonName,
                ProjectRole = vm.ProjectRole,
                IsLeadConsultant = vm.IsLeadConsultant,
            }, GetUserId(), User.IsInRole("Admin"), cancellationToken);

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
                PersonName = vm.PersonName,
                ProjectRole = vm.ProjectRole,
                IsLeadConsultant = vm.IsLeadConsultant,
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
}
