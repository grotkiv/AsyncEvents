namespace AsyncEvents.Tests;

using System.Diagnostics;
using System.Threading;
using AsyncEvents;
using Xunit.Abstractions;

public class AsyncEventHandlerTests
{
    private const string ThrowsAsyncMessage = "Throws Async";
    private const string ThrowsSyncMessage = "Throws Synchronous";

    private readonly ITestOutputHelper output;
    private readonly Stopwatch stopwatch;
    private event AsyncEventHandler<int>? Event;

    public AsyncEventHandlerTests(ITestOutputHelper output)
    {
        this.output = output;
        stopwatch = Stopwatch.StartNew();
    }

    [Fact]
    public async Task InvokeAsync_EventsRunInParallel()
    {
        Event += Delay2Seconds;
        Event += Delay3Seconds;

        await Event.InvokeAsync(this, 5);
        var now = stopwatch.Elapsed;
        Assert.Equal(0, now.Hours);
        Assert.Equal(0, now.Minutes);
        Assert.Equal(3, now.Seconds);
        output.WriteLine(now.ToString());
    }

    [Fact]
    public async Task InvokeAsync_ThrowsExceptionOfSubsriber_ButRunsAllOtherSubscriber()
    {
        bool called = false;
        Event += OnMyEventThrowsAsync;
        Event += (sender, e, cancellationToken) =>
        {
            called = true;
            LogFinished();
            return Task.CompletedTask;
        };

        await Assert.ThrowsAsync<NotImplementedException>(() => Event.InvokeAsync(this, 5));
        Assert.True(called);
        output.WriteLine(stopwatch.Elapsed.ToString());
    }

    [Fact]
    public async Task InvokeAsync_ThrowsAggregateException_WhenBothSubscribersThrowAfterAwait()
    {
        Event += OnMyEventThrowsAsync;
        Event += OnMyEventThrowsAsync;

        var exception = await Assert.ThrowsAsync<AggregateException>(() => Event.InvokeAsync(this, 5));
        Assert.Contains(ThrowsAsyncMessage, exception.Message);
        Assert.Contains(ThrowsAsyncMessage, exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_ThrowsAggregateException_WhenBothSubscribersThrowBeforeAwait()
    {
        Event += OnMyEventThrowsSynchronous;
        Event += OnMyEventThrowsSynchronous;

        var exception = await Assert.ThrowsAsync<AggregateException>(() => Event.InvokeAsync(this, 5));
        Assert.Contains(ThrowsSyncMessage, exception.Message);
        Assert.Contains(ThrowsSyncMessage, exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_ThrowsAggregateException_WhenOneSubscriberThrowsAsyncAndTheOtherSync()
    {
        Event += OnMyEventThrowsAsync;
        Event += OnMyEventThrowsSynchronous;

        var exception = await Assert.ThrowsAsync<AggregateException>(() => Event.InvokeAsync(this, 5).WithAggregateException());
        Assert.Contains(ThrowsAsyncMessage, exception.Message);
        Assert.Contains(ThrowsSyncMessage, exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_ThrowsTaskCanceledException_WhenTaskDelayIsCancelled()
    {
        Event += OnEventWaitForever;
        Event += OnEventWaitForever;

        CancellationTokenSource tokenSource = new();
        var task = Event.InvokeAsync(this, 6, tokenSource.Token);

        tokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact]
    public async Task InvokeAsync_ThrowsOperationCanceledException_WhenCancelledBeforeTaskRuns()
    {
        Event += OnEventWaitForever;
        Event += OnEventWaitForever;

        CancellationTokenSource tokenSource = new();
        tokenSource.Cancel();
        var task = Event.InvokeAsync(this, 6, tokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    private async Task OnEventWaitForever(object? sender, int e, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
    }

    private async Task Delay2Seconds(object? sender, int e, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        LogFinished();
    }

    private async Task Delay3Seconds(object? sender, int e, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3));
        LogFinished();
    }

    private void LogFinished()
    {
        output.WriteLine($"One subscriber finished after {stopwatch.Elapsed.TotalSeconds} seconds.");
    }

    private async Task OnMyEventThrowsAsync(object? sender, int e, CancellationToken cancellationToken)
    {
        output.WriteLine(ThrowsAsyncMessage);
        await Task.Delay(TimeSpan.FromMilliseconds(50));
        throw new NotImplementedException(ThrowsAsyncMessage);
    }

    private Task OnMyEventThrowsSynchronous(object? sender, int e, CancellationToken cancellationToken)
    {
        output.WriteLine(ThrowsSyncMessage);
        throw new NotImplementedException(ThrowsSyncMessage);
    }
}
