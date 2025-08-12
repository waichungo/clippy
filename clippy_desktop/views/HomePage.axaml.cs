using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Clippy.viewmodels;

namespace Clippy;

public partial class HomePage : UserControl
{
    public static HomePageViewModel model => new HomePageViewModel
    {
        SelectedDeviceIndex = 0,
        User= MainWindow.context.CurrentUser
    };
    public HomePage()
    {
        InitializeComponent();
        DataContext = model;
        
    }
}