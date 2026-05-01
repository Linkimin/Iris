using System;
using System.Globalization;

using Avalonia.Data.Converters;

using Iris.Desktop.Models;

namespace Iris.Desktop.Converters;

/// <summary>
/// Returns <c>true</c> when the bound <see cref="AvatarState"/> equals the converter parameter.
/// </summary>
public sealed class StateEqualityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AvatarState state && parameter is AvatarState target)
        {
            return state == target;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
