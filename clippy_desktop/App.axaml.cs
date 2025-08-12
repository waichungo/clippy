using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Clippy.db.functions;
using Clippy.viewmodels;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Linq;
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
    public static InternetChecker internetChecker = new InternetChecker();

    public static IObservable<Firebase.Database.Streaming.FirebaseEvent<Device>>? devicesWatcher = null;
    public static IObservable<Firebase.Database.Streaming.FirebaseEvent<string>>? clipWatcher = null;
    private static SemaphoreSlim clipWatcherInitializerLock = new SemaphoreSlim(1);
    public static void InitializeClipObserver(Device device)
    {
        try
        {
            clipWatcherInitializerLock.Wait();
            if (internetChecker.IsConnected && firebaseClient != null && MainWindow.context.CurrentUser != null)
            {
                clipWatcher = firebaseClient.Child(FirebaseUtil.getClipItemRefString(MainWindow.context.CurrentUser, device.Id)).AsObservable<string>();
                clipWatcher.Subscribe(json =>
                {
                    if (json.Object != null)
                    {
                        var clip = ClipItem.FromJSONString(json.Object);
                        if (clip.Device != Utils.GetSystemUuid())
                        {
                            ClipItemDBUtility.CreateClipItem(clip);
                        }
                    }
                });
            }            
        }
        finally
        {
            clipWatcherInitializerLock.Release();
        }

    }
    public async Task fireTask()
    {
        while (true)
        {
            if (internetChecker.IsConnected && firebaseClient != null && MainWindow.context.CurrentUser != null)
            {
                var unSynced = ClipItemDBUtility.FindClipItems(queryParam: new AndQuery
                {
                    Queries = [new QueryParam {
                        Key="synced",
                        EQUALITY=DBEQUALITY.EQUAL,
                        Value=false
                    }]
                }).Entries.Where(e => !e.Synced).ToList();
                foreach (var clipItem in unSynced)
                {
                    var clip = await FirebaseUtil.CreateClip(firebaseClient, MainWindow.context.CurrentUser, clipItem);
                    ClipItemDBUtility.UpdateClipItem(clip);
                }
                if (devicesWatcher == null)
                {
                    devicesWatcher = firebaseClient.Child(FirebaseUtil.getDevicesRefString(MainWindow.context.CurrentUser)).AsObservable<Device>();
                    devicesWatcher.Subscribe(e =>
                    {
                        if (e.Object != null)
                        {
                            if (!HomePage.model.Devices.Any(el => el.Id == e.Object.Id))
                            {
                                HomePage.model.Devices.Add(e.Object);
                            }
                        }
                    });
                }
                if (clipWatcher == null && internetChecker.IsConnected && firebaseClient != null && MainWindow.context.CurrentUser != null)
                {
                    InitializeClipObserver(HomePage.model.Devices[HomePage.model.SelectedDeviceIndex]);
                }

                var last = ClipItemDBUtility.FindClipItems(queryParam: new AndQuery
                {
                    Queries = [new QueryParam {
                        Key="device",
                        EQUALITY=DBEQUALITY.EQUAL,
                        Value=Utils.GetSystemUuid()
                    }]
                }, orderKey: "created_at", limit: 1, orderDescending: true

                ).Entries.FirstOrDefault();
                try
                {
                    var list = await FirebaseUtil.ListClips(firebaseClient, MainWindow.context.CurrentUser, HomePage.model.Devices[HomePage.model.SelectedDeviceIndex].Id, last?.Id);
                    if (list != null && list.Count > 0)
                    {
                        var loadedIds = HomePage.model.ClipItems.Select(el => el.Id).ToList();
                        bool added = false;
                        foreach (var item in list)
                        {
                            if (!loadedIds.Contains(item.Id))
                            {
                                HomePage.model.ClipItems.Add(item);
                                added = true;
                            }
                        }
                        if (added)
                        {
                            HomePage.model.ClipItems = new(HomePage.model.ClipItems.OrderBy(el => el.CreatedAt));
                        }
                    }
                }
                catch (Exception)
                {

                }

            }
            await Task.Delay(5000);
        }
    }
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
        internetChecker.Start();
        _ = Utils.ExecuteOnNewThread(() =>
        {
            _ = fireTask();
        });
    }



    private void MessageWindow_ClipboardUpdated(object? sender, System.EventArgs e)
    {

        var clip = ClipboardHelper.GetClipItem();
        if (clip != null)
        {
            clip = ClipItemDBUtility.CreateClipItem(clip);
            if (clip != null)
            {
                _ = Utils.ExecuteOnNewThread(async () =>
                {
                    if (HomePage.model.Devices[HomePage.model.SelectedDeviceIndex].Id == Utils.GetSystemUuid())
                    {
                        HomePage.model.ClipItems.Add(clip);
                    }

                    if (firebaseClient != null && MainWindow.context.CurrentUser != null)
                    {
                        clip = await FirebaseUtil.CreateClip(firebaseClient, MainWindow.context.CurrentUser, clip);
                        if (clip != null)
                        {
                            ClipItemDBUtility.UpdateClipItem(clip);
                        }
                    }

                });
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