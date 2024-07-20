namespace AsyncEvents;

public static class TaskExtensions
{
    /// <summary>
    /// A workaround for getting all of AggregateException.InnerExceptions with try/await/catch
    /// </summary>
    /// <see href="https://stackoverflow.com/questions/12007781/why-doesnt-await-on-task-whenall-throw-an-aggregateexception">Why doesn't await on Task.WhenAll throw an AggregateException?</see>
    public static Task WithAggregateException(this Task @this)
    {
        // using AggregateException.Flatten as a bonus
        return @this.ContinueWith(
            continuationFunction: anteTask =>
                anteTask.IsFaulted &&
                anteTask.Exception is AggregateException ex &&
                (ex.InnerExceptions.Count > 1 || ex.InnerException is AggregateException) ?
                Task.FromException(ex.Flatten()) : anteTask,
            cancellationToken: CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            scheduler: TaskScheduler.Default).Unwrap();
    }
}