using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tenderizer.Dtos;
using Tenderizer.Services.Interfaces;
using Tenderizer.ViewModels;

namespace Tenderizer.Controllers;

[Authorize(Roles = "Admin")]
[Route("library-documents")]
public sealed class LibraryDocumentsController : Controller
{
    private readonly ILibraryDocumentService _libraryDocumentService;
    private readonly IPrivateFileStore _privateFileStore;

    public LibraryDocumentsController(
        ILibraryDocumentService libraryDocumentService,
        IPrivateFileStore privateFileStore)
    {
        _libraryDocumentService = libraryDocumentService;
        _privateFileStore = privateFileStore;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vm = new LibraryDocumentsIndexVm
        {
            Items = await _libraryDocumentService.GetListAsync(cancellationToken),
        };

        return View(vm);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new LibraryDocumentCreateVm());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var vm = await _libraryDocumentService.GetDetailsAsync(id, cancellationToken);
            return View(vm);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LibraryDocumentCreateVm vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        if (vm.File is null)
        {
            ModelState.AddModelError(nameof(vm.File), "Select a file for the initial library document version.");
            return View(vm);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var id = await _libraryDocumentService.CreateAsync(new LibraryDocumentCreateDto
            {
                Name = vm.Name,
                Description = vm.Description,
                File = vm.File,
                ExpiryDateUtc = vm.ExpiryDateUtc,
            }, userId, cancellationToken);

            TempData["StatusMessage"] = "Library document created.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpPost("{id:guid}/versions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVersion(Guid id, LibraryDocumentVersionCreateVm vm, CancellationToken cancellationToken)
    {
        if (vm.File is null)
        {
            TempData["ErrorMessage"] = "Select a file for the new version.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await _libraryDocumentService.AddVersionAsync(id, new LibraryDocumentVersionCreateDto
            {
                File = vm.File,
                ExpiryDateUtc = vm.ExpiryDateUtc,
            }, userId, cancellationToken);

            TempData["StatusMessage"] = "New library document version uploaded.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet("{id:guid}/versions/{versionId:guid}/download")]
    public async Task<IActionResult> Download(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        try
        {
            var descriptor = await _libraryDocumentService.GetDownloadAsync(id, versionId, cancellationToken);
            var file = await _privateFileStore.OpenReadAsync(descriptor.StoredFile, descriptor.DownloadFileName, cancellationToken);
            return File(file.Stream, file.ContentType, file.DownloadFileName, enableRangeProcessing: true);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}
