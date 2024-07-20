namespace AsyncEvents;

public delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e, CancellationToken cancellationToken);

public static class AsyncEventHandlerExtensions
{
    /// <summary>
    /// Invokes and awaits all <see cref="AsyncEventHandler{TEventArgs}"> subscribed.
    /// </summary>
    /// <param name="asyncEventHandler">The <see cref="AsyncEventHandler{TEventArgs}"> event to invoke.</param>
    /// <param name="sender">The sender raising the event.</param>
    /// <param name="e">The event arguments to send.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="AggregateException">Thrown when more than one <see cref="AsyncEventHandler{TEventArgs}"> throws an exception.</exception>
    public static async Task InvokeAsync<TEventArgs>(this AsyncEventHandler<TEventArgs> asyncEventHandler, object? sender, TEventArgs e, CancellationToken cancellationToken = default)
    {
        if (asyncEventHandler is null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await Task.WhenAll(asyncEventHandler.GetInvocationList()
            .OfType<AsyncEventHandler<TEventArgs>>()
            .Select(eventHandler => InvokeSingleAsync(eventHandler, sender, e, cancellationToken)))
            .WithAggregateException()
            .ConfigureAwait(false);
    }

    private static Task InvokeSingleAsync<TEventArgs>(AsyncEventHandler<TEventArgs> asyncEventHandler, object? sender, TEventArgs e, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return asyncEventHandler(sender, e, cancellationToken);
        }
        catch (Exception exception)
        {
            // When the exception is thrown in the synchronous part of the async method,
            // no task is returned containing the exception but the exception is thrown.
            // This catches the exception and wraps it in a task.
            // The exception will be thrown by await or can be aggregated when using Task.WhenAll combined with WithAggregateException().
            return Task.FromException(exception);
        }
    }
}
