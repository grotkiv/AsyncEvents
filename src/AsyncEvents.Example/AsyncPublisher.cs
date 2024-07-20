namespace AsyncEvents.Example;

public sealed class AsyncPublisher
{
    public event AsyncEventHandler<int>? Event;

    public async Task FireAsync(int e, CancellationToken cancellationToken = default)
    {
        if (Event is null)
        {
            return;
        }

        try
        {
            await Event.InvokeAsync(this, e, cancellationToken);
        }
        catch (Exception)
        {
            // may handle exception from subscribers here
        }
    }
}
