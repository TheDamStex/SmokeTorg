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

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static string ToDisplay(UserRole role)
    {
        if (role == UserRole.Admin)
        {
            return "Адміністратор";
        }

        if (role == UserRole.Manager)
        {
            return "Керівник";
        }

        if (role == UserRole.Seller || role == UserRole.Cashier)
        {
            return "Продавець";
        }

        return role.ToString();
    }
}
