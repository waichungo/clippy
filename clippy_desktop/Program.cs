using Avalonia;
using Newtonsoft.Json;
using System;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Tmds.DBus.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;
using Clippy.db;
using Clippy.db.functions;

namespace Clippy;

class Program
{
    public static Mutex appLock = new(false, "Global Clippy desktop app");
    public static NamedPipe? MessagePipe = null;
    const string PipeName = "clippy app message pipe";
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {

        if (!appLock.WaitOne(TimeSpan.Zero))
        {
            if (!Utils.BringAppToFront())
            {
                PassArgsToFirstInstance(JsonConvert.SerializeObject(new List<string> { "bringToFront" }));
            }
            Environment.Exit(0);
        }

        if (args.Any(el => el.ToLower().Trim().Contains("hidden") || el.ToLower().Trim().Contains("silent")))
        {
            App.StartHidden = true;
        }
        Setup(args);
        var app = BuildAvaloniaApp();
        app.StartWithClassicDesktopLifetime(args);
    }

    static void Setup(string[] args)
    {
        DBAccess.Initialize();

       

        MainWindow.context.CurrentClipItems = new(ClipItemDBUtility.FindClipItems(orderKey: "updated_at", orderDescending: false).Entries);
        MessagePipe = new NamedPipe(PipeName, (message) =>
        {
            try
            {
                var args = JsonConvert.DeserializeObject<List<string>>(message);
                _ = Utils.ExecuteOnNewThread(() => ProcessCommandLine(args.ToArray()));
            }
            catch (Exception ex)
            {
            }
        });
        MessagePipe.Start();
        ProcessCommandLine(args);
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            CleanUp().Wait();
        };
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            CleanUp().Wait();
        };
    }
    static async Task CleanUp()
    {
        MessagePipe?.Stop();
        MessagePipe = null;
        App.messageWindow?.Dispose();
        App.messageWindow = null;
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
       => AppBuilder.Configure<App>()
           .UsePlatformDetect()
           .LogToTrace();
    public static void ProcessCommandLine(string[] args)
    {
        if (args.Contains("bringToFront"))
        {
            App.DisplayMainWindow();
        }
    }
    static void PassArgsToFirstInstance(string args)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut))
                {
                    var counts = 0;
                    using StreamWriter writer = new StreamWriter(pipeClient);
                    pipeClient.Connect();
                    do
                    {
                        try
                        {
                            writer.WriteLine(args);
                            writer.Flush();
                            return;
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine(string.Format("IOException  ERROR: {0}", ex.Message));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(string.Format("Exception ERROR: {0}", ex.Message));
                        }
                        counts++;
                        Thread.Sleep(200);
                    } while (counts < 5);
                }
            }
            catch (Exception ex1)
            {
                Console.WriteLine(string.Format("Exception outer loop - ", ex1.Message));
            }
            Thread.Sleep(200);
        }
    }

}
