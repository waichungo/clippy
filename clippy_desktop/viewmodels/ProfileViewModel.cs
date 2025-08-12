using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.viewmodels
{
    public partial class ProfileViewModel : ViewModelBase
    {
        [RelayCommand]
        public void Logout()
        {
            App.firebaseClient?.Dispose();
            App.firebaseClient = null;
            MainWindow.context.CurrentUser = null;
            MainWindow.context.CurrentPage = Page.LOGIN;
        }

    }
}
