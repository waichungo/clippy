using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.viewmodels
{
    public partial class SettingPageViewModel:ViewModelBase
    {
        [ObservableProperty]
        private bool _launchAtStartup = false;
    }
}
