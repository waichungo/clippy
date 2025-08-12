using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Clippy.viewmodels;

namespace Clippy;

public partial class ProfilePage : UserControl
{
    public static ProfileViewModel context = new();
    public ProfilePage()
    {
        InitializeComponent();
        DataContext = context;
    }
}