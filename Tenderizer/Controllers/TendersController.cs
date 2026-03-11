using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;
using Tenderizer.ViewModels;

namespace Tenderizer.Controllers;

[Authorize]
[Route("tenders")]
public sealed class TendersController : Controller
{
    private readonly ITenderService _tenderService;
    private readonly IUserLookupService _userLookupService;

    public TendersController(
        ITenderService tenderService,
        IUserLookupService userLookupService)
    {
        _tenderService = tenderService;
        _userLookupService = userLookupService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        var tenders = await _tenderService.GetListAsync(cancellationToken);
        var totalCount = tenders.Count;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            tenders = tenders
                .Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (t.Client?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        ViewData["TotalCount"] = totalCount;
        ViewData["Query"] = q?.Trim();
        return View(tenders);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = User.IsInRole("Admin");

        var vm = await _tenderService.GetDetailsAsync(id, userId, isAdmin, cancellationToken);
        return View(vm);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        await PopulateOwnerListAsync(isAdmin, cancellationToken);

        var vm = new TenderUpsertVm
        {
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            Status = TenderStatus.Draft,
        };

        if (!isAdmin)
        {
            vm.OwnerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenderUpsertVm vm, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = User.IsInRole("Admin");

        if (!ModelState.IsValid)
        {
            await PopulateOwnerListAsync(isAdmin, cancellationToken);
            return View(vm);
        }

        try
        {
            var id = await _tenderService.CreateAsync(ToDto(vm), userId, isAdmin, cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateOwnerListAsync(isAdmin, cancellationToken);
            return View(vm);
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = User.IsInRole("Admin");

        var details = await _tenderService.GetDetailsAsync(id, userId, isAdmin, cancellationToken);

        await PopulateOwnerListAsync(isAdmin, cancellationToken);

        var vm = new TenderUpsertVm
        {
            Name = details.Name,
            ReferenceNumber = details.ReferenceNumber,
            Client = details.Client,
            Category = details.Category,
            ClosingAtUtc = details.ClosingAtUtc,
            Status = details.Status,
            OwnerUserId = details.OwnerUserId,
        };

        return View(vm);
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TenderUpsertVm vm, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = User.IsInRole("Admin");

        if (!ModelState.IsValid)
        {
            await PopulateOwnerListAsync(isAdmin, cancellationToken);
            return View(vm);
        }

        try
        {
            await _tenderService.UpdateAsync(id, ToDto(vm), userId, isAdmin, cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateOwnerListAsync(isAdmin, cancellationToken);
            return View(vm);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = true;

        var vm = await _tenderService.GetDetailsAsync(id, userId, isAdmin, cancellationToken);
        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        await _tenderService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOwnerListAsync(bool isAdmin, CancellationToken cancellationToken)
    {
        if (!isAdmin)
        {
            ViewBag.OwnerUsers = new List<SelectListItem>();
            return;
        }

        ViewBag.OwnerUsers = await _userLookupService.GetUserSelectListAsync(cancellationToken);
    }

    private static TenderUpsertDto ToDto(TenderUpsertVm vm)
    {
        return new TenderUpsertDto
        {
            Name = vm.Name,
            ReferenceNumber = vm.ReferenceNumber,
            Client = vm.Client,
            Category = vm.Category,
            ClosingAtUtc = vm.ClosingAtUtc,
            Status = vm.Status,
            OwnerUserId = vm.OwnerUserId ?? string.Empty,
        };
    }
}
