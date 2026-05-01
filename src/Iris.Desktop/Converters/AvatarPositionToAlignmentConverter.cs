using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Layout;

using Iris.Desktop.Models;

namespace Iris.Desktop.Converters;

/// <summary>
/// Converts an <see cref="AvatarPosition"/> to a <see cref="HorizontalAlignment"/>
/// or <see cref="VerticalAlignment"/> depending on the <c>parameter</c>:
/// <c>"Horizontal"</c> returns HorizontalAlignment, <c>"Vertical"</c> returns VerticalAlignment.
/// </summary>
public sealed class AvatarPositionToAlignmentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AvatarPosition position)
        {
            return null;
        }

        var direction = parameter as string;

        if (string.Equals(direction, "Horizontal", StringComparison.OrdinalIgnoreCase))
        {
            return position switch
            {
                AvatarPosition.TopLeft => HorizontalAlignment.Left,
                AvatarPosition.BottomLeft => HorizontalAlignment.Left,
                AvatarPosition.TopRight => HorizontalAlignment.Right,
                AvatarPosition.BottomRight => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Right
            };
        }

        if (string.Equals(direction, "Vertical", StringComparison.OrdinalIgnoreCase))
        {
            return position switch
            {
                AvatarPosition.TopLeft => VerticalAlignment.Top,
                AvatarPosition.TopRight => VerticalAlignment.Top,
                AvatarPosition.BottomLeft => VerticalAlignment.Bottom,
                AvatarPosition.BottomRight => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Bottom
            };
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
