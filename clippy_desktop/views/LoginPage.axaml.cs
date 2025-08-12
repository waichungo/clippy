using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Clippy.viewmodels;

namespace Clippy;

public partial class LoginPage : UserControl
{
    public static LoginPageViewModel model=new LoginPageViewModel();
    public LoginPage()
    {
        InitializeComponent();
        DataContext = model;
    }
}