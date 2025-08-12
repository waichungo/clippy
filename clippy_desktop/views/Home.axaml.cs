using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Clippy.viewmodels;

namespace Clippy;

public partial class Home : UserControl
{
    public static HomeViewModel model=> new HomeViewModel();
    public Home()
    {
        InitializeComponent();
        DataContext = model;
    }
}