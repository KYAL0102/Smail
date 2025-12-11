using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Core.Models;

namespace SmailAvalonia.Converter;

public class TypeToIconConverter : IValueConverter
{
    private static readonly Bitmap SmsBitmap = new("avares://SmailAvalonia/Assets/Icons/sms.png");
    private static readonly Bitmap EmailBitmap = new("avares://SmailAvalonia/Assets/Icons/email.png");
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Bitmap? bitmap = value switch
        {
            TransmissionType.SMS => SmsBitmap,
            TransmissionType.Email => EmailBitmap,
            _ => null
        };

        return bitmap; // <-- Create a Bitmap from the URI
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
