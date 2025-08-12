using Clippy;
using System;
using System.Threading.Tasks;

public class InternetChecker
{
    private bool _stop = false;
    public event EventHandler<bool> InternetChanged;
    Task? checkTask = null;
    public bool IsConnected = false;
    public InternetChecker()
    {

    }
    public void Stop()
    {
        if (checkTask == null)
        {
            _stop = false;
            return;
        }
        try
        {
            _stop = true;
            checkTask?.Wait();
            checkTask = null;
        }
        catch { }
        finally
        {
            _stop = false;
        }
    }
    public void Start()
    {
        InternetChecker reference = this;
        if (checkTask != null)
        {
            Stop();
        }
        checkTask = Utils.ExecuteOnNewThread(async () =>
        {

            while (!_stop)
            {
                bool connected2 = Utils.IsInternetAvailable().Result;
                if (connected2 != IsConnected)
                {
                    IsConnected = connected2;
                    InternetChanged?.Invoke(reference, IsConnected);
                }
                await Task.Delay(5000);
            }

        });


    }
}
