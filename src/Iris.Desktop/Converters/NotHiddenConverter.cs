using System;
using System.Globalization;

using Avalonia.Data.Converters;

using Iris.Desktop.Models;

namespace Iris.Desktop.Converters;

/// <summary>
/// Returns <c>true</c> when the avatar is not in the <see cref="AvatarState.Hidden"/> state.
/// </summary>
public sealed class NotHiddenConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AvatarState state)
        {
            return state != AvatarState.Hidden;
        }

        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
