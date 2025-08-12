using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.converters
{
    public class HomeTabToPageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (MainWindow.context.CurrentPage == viewmodels.Page.SETTINGS)
            {
                return new SettingsPage();
            }
            if (MainWindow.context.CurrentPage == viewmodels.Page.HOME)
            {
                return new HomePage();
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
