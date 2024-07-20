namespace AsyncEvents.Example.Tests;

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