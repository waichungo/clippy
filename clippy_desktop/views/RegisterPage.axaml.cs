using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Clippy.viewmodels;

namespace Clippy;

public partial class RegisterPage : UserControl
{
    public static RegisterPageViewModel context => new RegisterPageViewModel();
    public RegisterPage()
    {
        InitializeComponent();
        DataContext = context;
    }
}