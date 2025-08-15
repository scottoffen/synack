using Synack.Exceptions;

namespace Synack.Extensions;

/// <summary>
/// Provides extension methods for validating <see cref="ServerOptions"/> instances.
/// </summary>
public static class ServerOptionsExtensions
{
    internal static readonly string MessageDuplicatePort = "Duplicate listener port: {0}.";

    /// <summary>
    /// Validates the specified <see cref="ServerOptions"/> and throws an exception if any validation issues are found.
    /// </summary>
    /// <param name="options">The <see cref="ServerOptions"/> instance to validate.</param>
    /// <exception cref="InvalidServerOptionsException">Thrown when validation issues exist.</exception>
    internal static void ValidateAndThrow(this ServerOptions options)
    {
        var issues = options.Validate();
        if (issues.Any())
        {
            throw new InvalidServerOptionsException(issues);
        }
    }

    /// <summary>
    /// Validates the specified <see cref="ServerOptions"/> and returns any validation issues found.
    /// </summary>
    /// <param name="options">The <see cref="ServerOptions"/> instance to validate.</param>
    /// <returns>An enumerable collection of validation issue messages.</returns>
    public static IEnumerable<string> Validate(this ServerOptions options)
    {
        var ports = new HashSet<int>();

        foreach (var listener in options.Listeners)
        {
            if (!ports.Add(listener.Port))
            {
                yield return string.Format(MessageDuplicatePort, listener.Port);
            }
        }
    }

    /// <summary>
    /// Adds a new listener to the server options using the specified configuration action.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ServerOptions WithListener(this ServerOptions options, Action<ListenerOptions> configure)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var listener = new ListenerOptions();
        configure(listener);
        options.AddListener(listener);
        return options;
    }
}
