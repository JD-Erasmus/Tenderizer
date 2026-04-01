namespace Tenderizer.ViewModels;

public sealed class TenderTableVm
{
    public string TableId { get; init; } = string.Empty;
    public IReadOnlyList<TenderListItemVm> Items { get; init; } = Array.Empty<TenderListItemVm>();
    public string EmptyMessage { get; init; } = "No items.";
    public bool EnableSearch { get; init; } = true;
    public bool EnablePaging { get; init; } = true;
    public bool EnableInfo { get; init; } = true;
    public bool EnableLengthChange { get; init; } = true;
    public int PageLength { get; init; } = 10;
    public string DefaultOrderColumn { get; init; } = "closingAtUtc";
    public string DefaultOrderDirection { get; init; } = "asc";
    public string SearchPlaceholder { get; init; } = "Search tenders";
}
