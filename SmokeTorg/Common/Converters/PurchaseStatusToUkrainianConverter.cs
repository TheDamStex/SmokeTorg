using System.Globalization;
using System.Windows.Data;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Common.Converters;

public class PurchaseStatusToUkrainianConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryGetStatus(value, out var status))
        {
            return "Усі статуси";
        }

        return ToDisplay(status);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryParseStatus(value, out var status))
        {
            if (IsNullableTarget(targetType))
            {
                return null;
            }

            return Binding.DoNothing;
        }

        return status;
    }

    public static string ToDisplay(DocumentStatus status)
    {
        switch (status)
        {
            case DocumentStatus.Draft:
                return "Чернетка";
            case DocumentStatus.Posted:
                return "Проведено";
            case DocumentStatus.Cancelled:
                return "Скасовано";
            default:
                return status.ToString();
        }
    }

    private static bool TryGetStatus(object? value, out DocumentStatus status)
    {
        if (value is DocumentStatus directStatus)
        {
            status = directStatus;
            return true;
        }

        if (value is DocumentStatus? nullableStatus && nullableStatus.HasValue)
        {
            status = nullableStatus.Value;
            return true;
        }

        if (value is string stringValue)
        {
            return TryParseStatus(stringValue, out status);
        }

        status = default;
        return false;
    }

    private static bool TryParseStatus(object? value, out DocumentStatus status)
    {
        if (value is not string rawValue)
        {
            status = default;
            return false;
        }

        var normalized = rawValue.Trim();

        switch (normalized)
        {
            case "Чернетка":
            case "Draft":
                status = DocumentStatus.Draft;
                return true;
            case "Проведено":
            case "Posted":
                status = DocumentStatus.Posted;
                return true;
            case "Скасовано":
            case "Cancelled":
                status = DocumentStatus.Cancelled;
                return true;
            default:
                return Enum.TryParse(normalized, true, out status);
        }
    }

    private static bool IsNullableTarget(Type targetType)
    {
        return Nullable.GetUnderlyingType(targetType) is not null;
    }
}
