using Tenderizer.ViewModels;

namespace Tenderizer.Views.Home;

public sealed record TenderSectionVm(string Title, IReadOnlyList<TenderListItemVm> Items);
