using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace SubTrackr.Desktop.Tests.Hosting;

/// <summary>
/// One STA thread running a dispatcher with SubTrackr's <see cref="App"/> resources loaded, shared
/// by every WPF test: WPF allows one Application per process. Work runs on it through
/// <see cref="RunAsync"/>. Nothing is ever shown where a person could see it.
/// </summary>
public static class WpfHost
{
    private static readonly Lazy<Dispatcher> Instance = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    public static Task RunAsync(Func<Task> work) => RunAsync(async () =>
    {
        await work();
        return true;
    });

    public static Task RunAsync(Action work) => RunAsync(() =>
    {
        work();
        return Task.FromResult(true);
    });

    public static Task<T> RunAsync<T>(Func<Task<T>> work)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Instance.Value.BeginInvoke(async () =>
        {
            try
            {
                completion.SetResult(await work());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        return completion.Task;
    }

    private static Dispatcher Start()
    {
        Dispatcher? dispatcher = null;
        ExceptionDispatchInfo? failure = null;
        using var ready = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
                app.InitializeComponent();
                dispatcher = Dispatcher.CurrentDispatcher;
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }

            ready.Set();
            if (failure is null)
            {
                Dispatcher.Run();
            }
        })
        {
            IsBackground = true,
            Name = "WPF test host",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        failure?.Throw();
        return dispatcher!;
    }
}
