using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Clippy.db.functions;
using Clippy.viewmodels;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Clippy;

public partial class App : Application
{
    public static bool StartHidden = false;
    public static MessageWindow? messageWindow = null;
    public static MainWindow? MainWindow = null;

    public static FirebaseClient? firebaseClient = null;
    public static FirebaseAuthClient firebaseAuthClient = FirebaseUtil.CreateClient();
    
    public static Device GetCurrentDevice()
    {
        return new Device
        {
            Id = Utils.GetSystemUuid(),
            Name = $"{Environment.UserName} ({Environment.MachineName}) Windows"
        };
    }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new AppViewModel();
        messageWindow = new MessageWindow();
        messageWindow.ClipboardUpdated += MessageWindow_ClipboardUpdated;
    }

    private void MessageWindow_ClipboardUpdated(object? sender, System.EventArgs e)
    {
        var clip = ClipboardHelper.GetClipItem();
        if (clip != null)
        {
            clip = ClipItemDBUtility.CreateClipItem(clip);
            if (clip != null) {
                MainWindow.context.CurrentClipItems.Add(clip);
            }
        }
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (!StartHidden)
            {
                var win = new MainWindow();
                desktop.MainWindow = win;
                MainWindow = win;
            }
            else
            {
                MainWindow = null;
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
    public void CloseApp()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.TryShutdown();
        }
        Environment.Exit(0);
    }
    static private SemaphoreSlim mutex = new SemaphoreSlim(1);
    public static void DisplayMainWindow()
    {
        Utils.ExecuteOnMainThread(async () =>
        {
            try
            {
                await mutex.WaitAsync();

                App.MainWindow = App.MainWindow ?? new MainWindow();
                App.MainWindow.Show();
                if (App.MainWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
                    App.MainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                App.MainWindow.Activate();
            }
            finally
            {
                mutex.Release();
            }
        });
    }
}