using System.Collections.ObjectModel;

namespace Synack.Tests;

public class ServerOptionsTests
{
    [Fact]
    public void CreateInstance_ReturnsExpectedDefaults_WhenCalled()
    {
        var o = ServerOptions.CreateInstance();

        o.IsSealed.ShouldBeFalse();
        o.MaxConcurrentRequests.ShouldBe(-1);
        o.MaxQueueCapacity.ShouldBe(1024);
        o.QueueOverflowMode.ShouldBe(QueueOverflowMode.Wait);
        o.RequestQueueTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        o.Listeners.ShouldNotBeNull();
        o.Listeners.Count.ShouldBe(1);
        o.Listeners[0].Port.ShouldBe(5000);
        o.Listeners[0].Prefixes.ShouldContain("/");
    }

    [Fact]
    public void CreateInstance_ReturnsDistinctInstances_WhenCalledTwice()
    {
        var a = ServerOptions.CreateInstance();
        var b = ServerOptions.CreateInstance();

        ReferenceEquals(a, b).ShouldBeFalse();

        a.MaxQueueCapacity = -1;
        b.MaxQueueCapacity.ShouldBe(1024);
    }

    [Fact]
    public void Listeners_ReturnsLiveReadOnlyView_WhenAddingListeners()
    {
        var o = new ServerOptions();
        var view1 = o.Listeners;
        view1.ShouldBeOfType<ReadOnlyCollection<ListenerOptions>>();
        view1.Count.ShouldBe(0);

        o.AddListener(new ListenerOptions { Port = 1234, Prefixes = ["/a"] });
        var view2 = o.Listeners;

        ReferenceEquals(view1, view2).ShouldBeFalse(); // property constructs a wrapper per call in current implementation
        view2.Count.ShouldBe(1);
        view2[0].Port.ShouldBe(1234);
        view2[0].Prefixes.ShouldContain("/a/");
    }

    [Fact]
    public void AddListener_ThrowsArgumentNullException_WhenListenerIsNull()
    {
        var o = new ServerOptions();
        var ex = Should.Throw<ArgumentNullException>(() => o.AddListener(null!));
        ex.ParamName.ShouldBe("listener");
    }

    [Fact]
    public void MaxConcurrentRequests_Throws_WhenZero()
    {
        var o = new ServerOptions();
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => o.MaxConcurrentRequests = 0);
        ex.ParamName.ShouldBe(nameof(ServerOptions.MaxConcurrentRequests));
        ex.Message.ShouldContain(ServerOptions.MessageConcurrencyCannotBeZero);
    }

    [Theory]
    [InlineData(-5, -1)]
    [InlineData(-1, -1)]
    public void MaxConcurrentRequests_SetsMinusOne_WhenNegative(int input, int expected)
    {
        var o = new ServerOptions { MaxConcurrentRequests = input };
        o.MaxConcurrentRequests.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(1024)]
    public void MaxConcurrentRequests_SetsValue_WhenPositive(int value)
    {
        var o = new ServerOptions { MaxConcurrentRequests = value };
        o.MaxConcurrentRequests.ShouldBe(value);
    }

    [Fact]
    public void MaxQueueCapacity_Throws_WhenZero()
    {
        var o = new ServerOptions();
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => o.MaxQueueCapacity = 0);
        ex.ParamName.ShouldBe(nameof(ServerOptions.MaxQueueCapacity));
        ex.Message.ShouldContain(ServerOptions.MessageQueueCapacityCannotBeZero);
    }

    [Theory]
    [InlineData(-5, -1)]
    [InlineData(-1, -1)]
    public void MaxQueueCapacity_SetsMinusOne_WhenNegative(int input, int expected)
    {
        var o = new ServerOptions { MaxQueueCapacity = input };
        o.MaxQueueCapacity.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(4096)]
    public void MaxQueueCapacity_SetsValue_WhenPositive(int value)
    {
        var o = new ServerOptions { MaxQueueCapacity = value };
        o.MaxQueueCapacity.ShouldBe(value);
    }

    [Fact]
    public void RequestQueueTimeout_Throws_WhenNegativeAndNotInfinite()
    {
        var o = new ServerOptions();
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => o.RequestQueueTimeout = TimeSpan.FromMilliseconds(-2));
        ex.ParamName.ShouldBe(nameof(ServerOptions.RequestQueueTimeout));
        ex.Message.ShouldContain(ServerOptions.MessageTimeoutMustBeNonNegativeOrInfinite);
    }


    [Fact]
    public void RequestQueueTimeout_Allows_InfiniteAndNonNegative()
    {
        var o = new ServerOptions();

        o.RequestQueueTimeout = TimeSpan.Zero;
        o.RequestQueueTimeout.ShouldBe(TimeSpan.Zero);

        o.RequestQueueTimeout = TimeSpan.FromSeconds(5);
        o.RequestQueueTimeout.ShouldBe(TimeSpan.FromSeconds(5));

        o.RequestQueueTimeout = Timeout.InfiniteTimeSpan; // == TimeSpan.FromMilliseconds(-1)
        o.RequestQueueTimeout.ShouldBe(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void QueueOverflowMode_SetsValue_WhenUnsealed()
    {
        var o = new ServerOptions();
        o.QueueOverflowMode = QueueOverflowMode.Drop;
        o.QueueOverflowMode.ShouldBe(QueueOverflowMode.Drop);
    }

    [Fact]
    public void Mutators_ThrowInvalidOperation_WhenSealed()
    {
        var o = new ServerOptions();
        o.Seal();

        var ex1 = Should.Throw<InvalidOperationException>(() => o.MaxConcurrentRequests = 2);
        ex1.Message.ShouldBe(ServerOptions.MessageOptionsAreSealed);

        var ex2 = Should.Throw<InvalidOperationException>(() => o.MaxQueueCapacity = 2);
        ex2.Message.ShouldBe(ServerOptions.MessageOptionsAreSealed);

        var ex3 = Should.Throw<InvalidOperationException>(() => o.QueueOverflowMode = QueueOverflowMode.Drop);
        ex3.Message.ShouldBe(ServerOptions.MessageOptionsAreSealed);

        var ex4 = Should.Throw<InvalidOperationException>(() => o.RequestQueueTimeout = TimeSpan.FromSeconds(1));
        ex4.Message.ShouldBe(ServerOptions.MessageOptionsAreSealed);

        var ex5 = Should.Throw<InvalidOperationException>(() => o.AddListener(new ListenerOptions()));
        ex5.Message.ShouldBe(ServerOptions.MessageOptionsAreSealed);
    }

    [Fact]
    public void Unseal_AllowsMutations_AfterSeal()
    {
        var o = new ServerOptions();
        o.Seal();
        o.Unseal();

        o.MaxConcurrentRequests = 3;
        o.MaxConcurrentRequests.ShouldBe(3);

        o.MaxQueueCapacity = 2048;
        o.MaxQueueCapacity.ShouldBe(2048);

        o.QueueOverflowMode = QueueOverflowMode.Drop;
        o.QueueOverflowMode.ShouldBe(QueueOverflowMode.Drop);

        o.RequestQueueTimeout = TimeSpan.FromSeconds(2);
        o.RequestQueueTimeout.ShouldBe(TimeSpan.FromSeconds(2));

        o.AddListener(new ListenerOptions { Port = 6000, Prefixes = ["/x"] });
        o.Listeners.Count.ShouldBe(1);
        o.Listeners.Single().Port.ShouldBe(6000);
    }

    [Fact]
    public void Seal_IsIdempotent_WhenCalledTwice()
    {
        var o = ServerOptions.CreateInstance();
        o.IsSealed.ShouldBeFalse();
        o.Seal();
        o.IsSealed.ShouldBeTrue();
        Should.NotThrow(() => o.Seal());
        o.IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void Unseal_IsIdempotent_WhenCalledTwice()
    {
        var o = ServerOptions.CreateInstance();
        o.Seal();
        o.Unseal();
        o.IsSealed.ShouldBeFalse();
        Should.NotThrow(() => o.Unseal());
        o.IsSealed.ShouldBeFalse();
    }
}
