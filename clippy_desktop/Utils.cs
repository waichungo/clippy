using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clippy
{
    public class Utils
    {
        static public bool BringAppToFront()
        {
            var process = Process.GetCurrentProcess();
            var handle = process.MainWindowHandle;

            var second = Process.GetProcesses(Environment.MachineName).Where(p =>
            {
                try
                {
                    return p.MainWindowHandle != IntPtr.Zero && p.MainModule.FileName == process.MainModule.FileName && p.Id != process.Id;
                }
                catch { }
                return false;
            }).FirstOrDefault();
            if (second != null)
            {
                var res = Native.ShowWindow(second.MainWindowHandle, Native.SW_NORMAL);
                res = Native.SetForegroundWindow(second.MainWindowHandle);
                return res;
            }
            return false;
        }
        public static string GetSystemUuid()
        {
            string systemUuid = string.Empty;
            ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
            ManagementObjectCollection mbsList = mos.Get();
            foreach (ManagementBaseObject mo in mbsList)
            {
                systemUuid = mo["UUID"] as string;
                break;
            }
            return systemUuid;
        }
        public static async Task<bool> IsInternetAvailable()
        {
            using HttpClient client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://www.google.com");
            client.Timeout = TimeSpan.FromSeconds(5);
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    using HttpResponseMessage response = await client.SendAsync(request);
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                }
                await Task.Delay(200);
            }
            return false;
        }
        public static void ExecuteOnMainThread(Action action)
        {
            try
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    action();
                }
                else
                {
                    Dispatcher.UIThread.Invoke(action);
                }
            }
            catch (Exception)
            {
            }

        }
        public async static Task ExecuteOnNewThread(Action action)
        {
            var thread = new Thread(() => { action.Invoke(); });
            thread.Start();
            while (thread.IsAlive)
            {
                await Task.Delay(200);
            }
        }
    }
}
