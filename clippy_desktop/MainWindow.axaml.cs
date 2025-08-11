using Avalonia.Controls;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System;
using Clippy.viewmodels;

namespace Clippy;

public partial class MainWindow : Window
{
    public static MainWindowViewModel context = new MainWindowViewModel();
    public MainWindow()
    {
        InitializeComponent();
        Closing += (s, e) =>
        {
            App.MainWindow = null;
        };
        App.MainWindow = this;
        DataContext = context;
    }

}
