using Synack.Extensions;
using Synack.Exceptions;

namespace Synack.Tests.Extensions;

public sealed class ServerOptionsExtensionsTests
{
    [Fact]
    public void Validate_ShouldReturnEmpty_WhenDefaultsAreUsed()
    {
        var options = new ServerOptions();

        var result = options.Validate();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ShouldReturnIssue_WhenListenerHasInvalidPort()
    {
        var options = new ServerOptions();
        options.Listeners.Add(new ListenerOptions { Port = -1 });

        var result = options.Validate().ToList();

        result.ShouldContain("Listener port -1 is out of range.");
    }

    [Fact]
    public void Validate_ShouldReturnIssue_WhenDuplicatePortsExist()
    {
        var options = ServerOptions.Default;
        options.Listeners.Add(new ListenerOptions { Port = 5000 });

        var result = options.Validate().ToList();

        result.ShouldContain("Duplicate listener port: 5000.");
    }

    [Fact]
    public void Validate_ShouldReturnIssue_WhenListenerPrefixesAreEmpty()
    {
        var options = ServerOptions.Default;
        options.Listeners[0].Prefixes = [];

        var result = options.Validate().ToList();

        result.ShouldContain("Listener prefixes cannot be null or empty.");
    }

    [Fact]
    public void ValidateAndThrow_ShouldThrowInvalidServerOptionsException_WhenIssuesExist()
    {
        var options = ServerOptions.Default;
        options.Listeners[0].Prefixes = [];

        var exception = Should.Throw<InvalidServerOptionsException>(() => options.ValidateAndThrow());

        exception.Issues.ShouldContain("Listener prefixes cannot be null or empty.");
    }

    [Fact]
    public void ValidateAndThrow_ShouldNotThrow_WhenOptionsAreValid()
    {
        var options = new ServerOptions();

        Should.NotThrow(() => options.ValidateAndThrow());
    }
}
