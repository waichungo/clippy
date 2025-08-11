using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
namespace Clippy;
public class NamedPipe
{
    private string PipeName;

    public NamedPipe(string PipeName, Action<string>? onMessage = null)
    {
        this.PipeName = PipeName;
        this.onMessage = onMessage;
    }
    public Action<string>? onMessage = null;
    private bool KeepRunning { get; set; } = true;
    public void Stop()
    {
        KeepRunning = false;
        try
        {
            server?.Dispose();
        }
        catch (Exception)
        {
        }
    }
    private volatile bool running = false;
    public bool IsRunning { get { return running; } }
    NamedPipeServerStream server = null;
    private void StartPipeThread()
    {
        if (running) return;
        var thread = new Thread(() =>
        {
            running = true;
            var job = Task.Run(async () =>
            {
                do
                {
                    try
                    {
                        using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 5, PipeTransmissionMode.Message))
                        {
                            server = pipeServer;
                            using StreamReader Reader = new StreamReader(pipeServer);
                            var task = pipeServer.WaitForConnectionAsync();
                            while (!task.IsCompleted && KeepRunning)
                            {
                                await Task.Delay(250);
                            }
                            if (!KeepRunning) return;
                            do
                            {
                                try
                                {
                                    string? message = Reader.ReadLine();
                                    if (message != null)
                                    {
                                        onMessage?.Invoke(message);
                                    }
                                }
                                catch (IOException ex)
                                {
                                    Console.WriteLine(string.Format("IOException  ERROR: {0}", ex.Message));
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(string.Format("Exception ERROR: {0}", ex.Message));
                                    break;
                                }
                            } while (pipeServer.IsConnected && KeepRunning);
                        }
                        server = null;
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine(string.Format("Exception outer loop - ", ex1.Message));
                    }
                    Thread.Sleep(200);
                } while (KeepRunning);
                running = false;
            });
            job.Wait();
        });
        thread.Start();
    }

    public void Start()
    {
        StartPipeThread();
    }
}