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
    public class NameToInitialsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is String text)
            {
                return string.Join("",Regex.Split(text, @"(\s)+").Select(e => e.Trim()).Where(e => e.Length > 0).Take(2).Select(e => e[0].ToString().ToUpper()));
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
