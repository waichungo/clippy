using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.viewmodels
{
    public partial class AppSettings:ViewModelBase
    {
        [ObservableProperty]
        private bool _captureClips = true; 
        [ObservableProperty]
        private bool _captureImageClips = true; 
        [ObservableProperty]
        private bool _saveLoginInfo = true;
        [ObservableProperty]
        private bool _launchAtStartUp = true; 
        [ObservableProperty]
        private Device? lastSelectedDevice = null;
        [ObservableProperty]
        private Dictionary<string,string> devicePositions = new();
    }
}
