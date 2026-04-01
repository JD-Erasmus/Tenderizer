namespace Tenderizer.ViewModels;

public sealed class TenderListVm
{
    public IReadOnlyList<TenderListItemVm> Items { get; init; } = Array.Empty<TenderListItemVm>();
    public int TotalCount { get; init; }
}
