using Avalonia.Data.Converters;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Data.Entity.Infrastructure.Design.Executor;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Clippy.converters
{
    public class TimeToTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            DateTime? date = null;
            if (value is ClipItem clip)
            {
                date = clip.UpdatedAt;
            }
            else if (value is DateTime dt)
            {
                date = dt;
            }
            if (date != null)
            {
              return  Humanizer.DateHumanizeExtensions.Humanize(date, utcDate: null, dateToCompareAgainst: DateTime.Now);
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
