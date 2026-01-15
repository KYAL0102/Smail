using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Core.Models;
using System;
using System.Globalization;

namespace SmailAvalonia.Converter  // ← adjust to your actual namespace
{
    public class TypeToIconConverter : IValueConverter
    {
        // Optional: Make it a singleton for better performance (common pattern in Avalonia)
        public static readonly TypeToIconConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Optional safety: If Application.Current is null (very rare after app startup),
            // return a fallback geometry or null
            if (Application.Current == null)
                return null; // or return some default StreamGeometry

            if (value is TransmissionType type)
            {
                return type switch
                {
                    TransmissionType.Email => Application.Current.FindResource("mail_regular"),
                    TransmissionType.SMS => Application.Current.FindResource("sms_regular"),
                    _ => Application.Current.FindResource("mail_regular") // fallback to mail
                };
            }

            // Fallback if value isn't a string
            return Application.Current.FindResource("mail_regular");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException(); // One-way binding, so no need
    }
}