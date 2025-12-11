using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Core.Models;

namespace SmailAvalonia.Converter;

public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            SendStatus.PENDING => Brushes.Gray,
            SendStatus.PROCESSED => Brushes.LightBlue,
            SendStatus.SENT => Brushes.Blue,
            SendStatus.DELIVERED => Brushes.Green,
            SendStatus.FAILED => Brushes.Red,
            _ => Brushes.Transparent
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
