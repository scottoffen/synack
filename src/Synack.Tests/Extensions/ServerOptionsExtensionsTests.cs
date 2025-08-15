using Synack.Exceptions;
using Synack.Extensions;

namespace Synack.Tests.Extensions;

public class ServerOptionsExtensionsTests
{
    [Fact]
    public void Validate_ReturnsEmpty_WhenPortsAreUnique()
    {
        var options = new ServerOptions()
            .WithListener(l => l.Port = 8080)
            .WithListener(l => l.Port = 8081)
            .WithListener(l => l.Port = 0); // ephemeral ok

        var issues = options.Validate().ToArray();

        issues.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ReturnsIssues_ForDuplicatePorts()
    {
        var options = new ServerOptions()
            .WithListener(l => l.Port = 8080)
            .WithListener(l => l.Port = 8080) // duplicate #1
            .WithListener(l => l.Port = 8081)
            .WithListener(l => l.Port = 8080); // duplicate #2

        var issues = options.Validate().ToArray();

        issues.Length.ShouldBe(2);
        issues.ShouldAllBe(i => i.Contains("Duplicate listener port: 8080.", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateAndThrow_Throws_InvalidServerOptionsException_WhenIssuesExist()
    {
        var options = new ServerOptions()
            .WithListener(l => l.Port = 5000)
            .WithListener(l => l.Port = 5000);

        var ex = Should.Throw<InvalidServerOptionsException>(() => options.ValidateAndThrow());
        ex.Issues.ShouldContain("Duplicate listener port: 5000.");
    }

    [Fact]
    public void WithListener_AddsConfiguredListener_AndReturnsOptions()
    {
        var options = new ServerOptions();

        var returned = options.WithListener(l =>
        {
            l.Port = 7000;
            l.AddPrefix("/api/");
        });

        ReferenceEquals(returned, options).ShouldBeTrue();
        options.Listeners.Count.ShouldBe(1);
        options.Listeners[0].Port.ShouldBe(7000);
        options.Listeners[0].Prefixes.ShouldContain("/api/");
    }

    [Fact]
    public void WithListener_Throws_ArgumentNullException_WhenOptionsIsNull()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            ServerOptionsExtensions.WithListener(null!, _ => { }));

        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void WithListener_Throws_ArgumentNullException_WhenConfigureIsNull()
    {
        var options = new ServerOptions();

        var ex = Should.Throw<ArgumentNullException>(() =>
            ServerOptionsExtensions.WithListener(options, null!));

        ex.ParamName.ShouldBe("configure");
    }
}
