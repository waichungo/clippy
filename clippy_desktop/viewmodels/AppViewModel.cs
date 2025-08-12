using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.viewmodels
{
   
    public partial class AppViewModel : ViewModelBase
    {
      
        [RelayCommand]
        public async Task ShowWindow()
        {
            App.DisplayMainWindow();

        }
        [RelayCommand]
        public void Exit()
        {
            var app = (App?)App.Current;
            app?.CloseApp();
        }
    }
}
