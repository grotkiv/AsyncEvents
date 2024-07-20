
namespace AsyncEvents.Example;

public sealed class AsyncSubscriber : IDisposable
{
    private readonly AsyncPublisher asyncPublisher;

    public AsyncSubscriber(AsyncPublisher asyncPublisher)
    {
        this.asyncPublisher = asyncPublisher;
        asyncPublisher.Event += OnEvent;
    }

    public int LastEventArgs { get; set; }

    private async Task OnEvent(object? sender, int e, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        LastEventArgs = e;
    }

    public void Dispose()
    {
        asyncPublisher.Event -= OnEvent;
    }
}