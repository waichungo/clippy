using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.viewmodels
{
    public partial class RegisterPageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _email = ""; 
        [ObservableProperty]
        private string _name = ""; 
        [ObservableProperty]
        private string _password = "";
        [ObservableProperty]
        private string _confirmPassword = "";
        [RelayCommand]
        public async Task GoToLoginPage()
        {
            MainWindow.context.CurrentPage = Page.LOGIN;
        }
        [RelayCommand]
        public async Task Register()
        {
            try
            {
                var userCredential = await App.firebaseAuthClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Name);
                if (userCredential != null)
                {
                    MainWindow.context.CurrentPage = Page.LOGIN;
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
            }
            
            
            //var userCredential = await App.firebaseAuthClient .CreateUserWithEmailAndPasswordAsync(Email, Password, "Display Name");
        } 
    }
}
