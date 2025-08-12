using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Clippy.converters
{
    public class ClipTypeToContentConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ClipItem clip)
            {
                if (clip.ExType == ClipType.TEXT)
                {
                    //var text = Regex.Replace(clip.Data, @"(\r\n|\n)+", " ");
                    //return text;
                    return clip.Data;
                }
                else
                {
                   return "Image data";
                }
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
