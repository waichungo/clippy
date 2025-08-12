using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.viewmodels
{
    public enum Page
    {
        HOME,
        SETTINGS,
        PROFILE,
        LOGIN,
        REGISTER
    }
    public partial class User : ViewModelBase
    {
        [ObservableProperty]
        private string _name = "";
        [ObservableProperty]
        private string _email = "";
        [ObservableProperty]
        private string _id = "";
        [ObservableProperty]
        private string _password = "";

    }
    public partial class Device : ViewModelBase
    {
        [ObservableProperty]
        [property: JsonProperty("id")]
        private string _id = "";
        [ObservableProperty]
        [property: JsonProperty("name")]
        private string _name = "";
        [ObservableProperty]
        [property: JsonProperty("lastSystemTime")]
        private DateTime lastSystemTime = DateTime.Now;
    }
    public partial class MainWindowViewModel : ViewModelBase
    {

        [ObservableProperty]
        private User? _currentUser = null;
        [ObservableProperty]
        private Page _currentPage = Page.HOME;
        [RelayCommand]
        public void GoToHome()
        {
            if (CurrentPage != Page.HOME)
            {
                CurrentPage = Page.HOME;
            }
        }
    }
}
