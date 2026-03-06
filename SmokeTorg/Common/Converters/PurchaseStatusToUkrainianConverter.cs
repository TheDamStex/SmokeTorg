using System.Globalization;
using System.Windows.Data;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Common.Converters;

public class PurchaseStatusToUkrainianConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return "Усі статуси";
        }

        if (value is DocumentStatus status)
        {
            return ToDisplay(status);
        }

        if (value is DocumentStatus? nullableStatus && nullableStatus.HasValue)
        {
            return ToDisplay(nullableStatus.Value);
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static string ToDisplay(DocumentStatus status)
    {
        return status switch
        {
            DocumentStatus.Draft => "Чернетка",
            DocumentStatus.Posted => "Проведено",
            DocumentStatus.Cancelled => "Скасовано",
            _ => status.ToString()
        };
    }
}
