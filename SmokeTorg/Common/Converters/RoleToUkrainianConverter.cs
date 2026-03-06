using System.Globalization;
using System.Windows.Data;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Common.Converters;

public class RoleToUkrainianConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserRole role)
        {
            return ToDisplay(role);
        }

        if (value is UserRole? nullableRole && nullableRole.HasValue)
        {
            return ToDisplay(nullableRole.Value);
        }

        return "Усі ролі";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static string ToDisplay(UserRole role) => role switch
    {
        UserRole.Admin => "Адміністратор",
        UserRole.Manager => "Керівник",
        UserRole.Seller => "Продавець",
        UserRole.Cashier => "Продавець",
        _ => role.ToString()
    };
}
