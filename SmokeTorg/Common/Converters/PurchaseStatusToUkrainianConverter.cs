using System.Globalization;
using System.Windows.Data;
using PurchaseStatus = SmokeTorg.Domain.Enums.DocumentStatus;

namespace SmokeTorg.Common.Converters;

public class PurchaseStatusToUkrainianConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PurchaseStatus status)
        {
            return ToUkrainian(status);
        }

        if (value is string text)
        {
            if (TryParseStatus(text, out var parsedStatus))
            {
                return ToUkrainian(parsedStatus);
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PurchaseStatus status)
        {
            return status;
        }

        if (value is string text)
        {
            if (TryParseStatus(text, out var parsedStatus))
            {
                return parsedStatus;
            }
        }

        return Binding.DoNothing;
    }

    private static string ToUkrainian(PurchaseStatus status)
    {
        switch (status)
        {
            case PurchaseStatus.Draft:
                return "Чернетка";
            case PurchaseStatus.Posted:
                return "Проведено";
            case PurchaseStatus.Cancelled:
                return "Скасовано";
            default:
                return string.Empty;
        }
    }

    private static bool TryParseStatus(string value, out PurchaseStatus status)
    {
        var normalized = value.Trim();

        switch (normalized)
        {
            case "Чернетка":
            case "Draft":
                status = PurchaseStatus.Draft;
                return true;
            case "Проведено":
            case "Posted":
                status = PurchaseStatus.Posted;
                return true;
            case "Скасовано":
            case "Cancelled":
                status = PurchaseStatus.Cancelled;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
