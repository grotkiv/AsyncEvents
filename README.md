# AsyncEvents

Implements an `AsyncEventHandler` inspired by [The Hitchhiker Guide To Asynchronous Events in C#](https://medium.com/@a.lyskawa/the-hitchhiker-guide-to-asynchronous-events-in-c-e9840109fb53).

## Usage

See [AsyncEvents.Example](src/AsyncEvents.Example) and [PubSubExampleTest](tests/AsyncEvents.Example.Tests/PubSubExampleTest.cs):

1. Define the async event in the publisher class:

   ``` csharp
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
   ```

2. Subscribe to the event in any subsriber class, e.g.:

   ``` csharp
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
   ```

3. Put them together:

   ``` csharp
   public class PubSubExampleTest
   {
       [Fact]
       public async Task Test1()
       {
           var pub = new AsyncPublisher();
           var sub = new AsyncSubscriber(pub);

           await pub.FireAsync(4711);

           Assert.Equal(4711, sub.LastEventArgs);
       }
   }
   ```