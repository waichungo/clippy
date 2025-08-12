using Avalonia.Data.Converters;
using Clippy.viewmodels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.converters
{
    public class PageToControlConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Page page)
            {
                if (page == Page.HOME || page == Page.SETTINGS)
                {
                    return new Home();
                }
                if (page == Page.LOGIN)
                {
                    if (MainWindow.context.CurrentUser != null)
                    {
                        return new ProfilePage();
                    }
                    return new LoginPage();
                }
                if (page == Page.PROFILE)
                {
                    if (MainWindow.context.CurrentUser == null)
                    {
                        return new LoginPage();
                    }
                    return new ProfilePage();
                }
                if (page == Page.REGISTER)
                {
                    return new RegisterPage();
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
