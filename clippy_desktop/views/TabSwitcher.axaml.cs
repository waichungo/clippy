using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Clippy.viewmodels;
using System.Linq;

namespace Clippy;

public partial class TabSwitcher : UserControl
{
    private TabSwitcherViewModel context = new TabSwitcherViewModel
    {
        IsHomeActive = MainWindow.context.CurrentPage == Page.HOME
    };
    public TabSwitcher()
    {
        InitializeComponent();
        DataContext = context;
        Loaded += (s, e) =>
        {
            var toggles = this.GetVisualDescendants().OfType<ToggleButton>().Where(t => t.Classes.Any(c => c == "main-nav-toggle"));
            foreach (var toggle in toggles)
            {
                toggle.Click += (s, e) =>
                {
                    var button = (ToggleButton)s;
                    //var page = (Page)(button).Tag;
                    button.IsChecked = true;
                    if (toggle.Classes.Any(e => e.Contains("home")))
                    {
                        if (MainWindow.context.CurrentPage != Page.HOME)
                            MainWindow.context.CurrentPage = Page.HOME;
                    }
                    else
                    {
                        if (MainWindow.context.CurrentPage != Page.SETTINGS)
                            MainWindow.context.CurrentPage = Page.SETTINGS;
                    }
                };
                if (toggle.Classes.Any(e => e.Contains("home")))
                {
                    if (MainWindow.context.CurrentPage == Page.HOME)
                        toggle.IsChecked = true;
                }
                else
                {
                    if (MainWindow.context.CurrentPage == Page.SETTINGS)
                        toggle.IsChecked = true;
                }

            }
        };
    }
}