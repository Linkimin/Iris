using System;
using System.Globalization;

using Avalonia.Data.Converters;

using Iris.Desktop.Models;

namespace Iris.Desktop.Converters;

/// <summary>
/// Converts an <see cref="AvatarSize"/> value to a pixel dimension.
/// Small = 80, Medium = 120, Large = 180.
/// </summary>
public sealed class AvatarSizeToPixelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AvatarSize size)
        {
            return size switch
            {
                AvatarSize.Small => 80.0,
                AvatarSize.Medium => 120.0,
                AvatarSize.Large => 180.0,
                _ => 120.0
            };
        }

        return 120.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
