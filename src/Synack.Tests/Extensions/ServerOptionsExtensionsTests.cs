using Synack.Extensions;
using Synack.Exceptions;

namespace Synack.Tests;

public class ServerOptionsExtensionsTests
{
    [Fact]
    public void Validate_ReturnsEmpty_ForValidDefaults()
    {
        var o = ServerOptions.CreateInstance();
        var issues = o.Validate().ToArray();
        issues.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ReturnsIssue_ForNegativePort()
    {
        var o = new ServerOptions();
        o.AddListener(new ListenerOptions { Port = -1, Prefixes = ["/"] });

        var issues = o.Validate().ToArray();
        issues.Length.ShouldBe(1);
        issues[0].ShouldBe("Listener port -1 is out of range.");
    }

    [Fact]
    public void Validate_ReturnsIssue_ForDuplicatePorts()
    {
        var o = new ServerOptions();
        o.AddListener(new ListenerOptions { Port = 5000, Prefixes = ["/a"] });
        o.AddListener(new ListenerOptions { Port = 5000, Prefixes = ["/b"] });

        var issues = o.Validate().ToArray();
        issues.Length.ShouldBe(1);
        issues[0].ShouldBe("Duplicate listener port: 5000.");
    }

    [Fact]
    public void Validate_ReturnsIssue_ForNullOrEmptyPrefixes()
    {
        var o = new ServerOptions();

        o.AddListener(new ListenerOptions { Port = 6001, Prefixes = null! });
        o.Validate().Single().ShouldBe("Listener prefixes cannot be null or empty.");

        var o2 = new ServerOptions();
        o2.AddListener(new ListenerOptions { Port = 6002, Prefixes = new List<string>() });
        o2.Validate().Single().ShouldBe("Listener prefixes cannot be null or empty.");

        var o3 = new ServerOptions();
        o3.AddListener(new ListenerOptions { Port = 6003, Prefixes = ["  "] });
        o3.Validate().Single().ShouldBe("Listener prefixes cannot be null or empty.");
    }

    [Fact]
    public void Validate_ReturnsMultipleIssues_WhenMultipleProblemsExist()
    {
        var o = new ServerOptions();

        o.AddListener(new ListenerOptions { Port = -1, Prefixes = null! });   // two issues
        o.AddListener(new ListenerOptions { Port = -1, Prefixes = [] });      // two issues + duplicate port

        var issues = o.Validate().ToArray();
        issues.ShouldContain("Listener port -1 is out of range.");
        issues.ShouldContain("Listener prefixes cannot be null or empty.");
        issues.ShouldContain("Duplicate listener port: -1.");
    }

    [Fact]
    public void ValidateAndThrow_Throws_InvalidServerOptionsException_WhenIssuesExist()
    {
        var o = new ServerOptions();
        o.AddListener(new ListenerOptions { Port = -1, Prefixes = ["/"] });

        var ex = Should.Throw<InvalidServerOptionsException>(() => o.ValidateAndThrow());

        ex.Issues.ShouldNotBeNull();

        var issues = ex.Issues;
        issues.ShouldNotBeNull();
        issues!.ShouldContain("Listener port -1 is out of range.");
    }


    [Fact]
    public void ValidateAndThrow_DoesNotThrow_WhenNoIssues()
    {
        var o = ServerOptions.CreateInstance();
        Should.NotThrow(() => o.ValidateAndThrow());
    }

    [Fact]
    public void WithListener_AddsConfiguredListener_AndReturnsSameInstance()
    {
        var o = new ServerOptions();

        var returned = o.WithListener(l =>
        {
            l.Port = 7001;
            l.Prefixes = ["/x"];
        });

        ReferenceEquals(returned, o).ShouldBeTrue();
        o.Listeners.Count.ShouldBe(1);
        o.Listeners[0].Port.ShouldBe(7001);
        o.Listeners[0].Prefixes.ShouldContain("/x");
    }

    [Fact]
    public void WithListener_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        ServerOptions? o = null;
        var ex = Should.Throw<ArgumentNullException>(() => ServerOptionsExtensions.WithListener(o!, _ => { }));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void WithListener_ThrowsArgumentNullException_WhenConfigureIsNull()
    {
        var o = new ServerOptions();
        var ex = Should.Throw<ArgumentNullException>(() => o.WithListener(null!));
        ex.ParamName.ShouldBe("configure");
    }

    [Fact]
    public void Validate_DoesNotReportIssues_ForDifferentPorts()
    {
        var o = new ServerOptions();
        o.AddListener(new ListenerOptions { Port = 5000, Prefixes = ["/a"] });
        o.AddListener(new ListenerOptions { Port = 5001, Prefixes = ["/b"] });

        var issues = o.Validate().ToArray();
        issues.ShouldBeEmpty();
    }
}
