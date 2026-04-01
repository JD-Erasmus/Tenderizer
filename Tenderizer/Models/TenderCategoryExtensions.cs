using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Tenderizer.Models;

public static class TenderCategoryExtensions
{
    public static string ToStorageValue(this TenderCategory category)
    {
        var member = typeof(TenderCategory).GetMember(category.ToString()).FirstOrDefault();
        var displayName = member?.GetCustomAttribute<DisplayAttribute>()?.GetName();
        return string.IsNullOrWhiteSpace(displayName) ? category.ToString() : displayName;
    }

    public static TenderCategory? ParseOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (var category in Enum.GetValues<TenderCategory>())
        {
            if (string.Equals(category.ToString(), value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(category.ToStorageValue(), value, StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        return null;
    }
}
