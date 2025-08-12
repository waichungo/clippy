using Clippy.db.functions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.viewmodels
{
    public partial class LoginPageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _email = "waichungojames@gmail.com";
        [ObservableProperty]
        private string _password = "jkuat123"; 
        [ObservableProperty]
        private bool _saveInfo= false;
        [RelayCommand]
        public async Task GoToRegisterPage()
        {
            MainWindow.context.CurrentPage = Page.REGISTER;
        }
        [RelayCommand]
        public async Task Login()
        {
            try
            {
                var userCredential = await App.firebaseAuthClient.SignInWithEmailAndPasswordAsync(Email, Password);
                if (userCredential != null)
                {

                    MainWindow.context.CurrentUser = new User
                    {
                        Email = userCredential.User.Info.Email,
                        Name = userCredential.User.Info.DisplayName,
                        Id = userCredential.User.Uid,
                    };
                    var client = await FirebaseUtil.GetFirebaseClient(userCredential.User.Credential.IdToken);
                    App.firebaseClient = client;
                    App.InitializeClipObserver(HomePage.model.Devices[HomePage.model.SelectedDeviceIndex]);
                    await FirebaseUtil.PostDevice(client, MainWindow.context.CurrentUser, App.GetCurrentDevice());
                    //var lastClip=ClipItemDBUtility.FindClipItems(limit:1,orderDescending:true,orderKey: "updated_at").Entries.FirstOrDefault();                   
                    //var list =await FirebaseUtil.ListClips(App.firebaseClient, MainWindow.context.CurrentUser, lastClip?.Id);
                   
                    MainWindow.context.CurrentPage = Page.HOME;

                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //var userCredential = await App.firebaseAuthClient .CreateUserWithEmailAndPasswordAsync(Email, Password, "Display Name");
        }
    }
}
