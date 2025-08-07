using Synack.Exceptions;

namespace Synack.Extensions;

/// <summary>
/// Provides extension methods for validating <see cref="ServerOptions"/> instances.
/// </summary>
public static class ServerOptionsExtensions
{
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
            if (listener.Port < 0 || listener.Port > int.MaxValue)
            {
                yield return $"Listener port {listener.Port} is out of range.";
            }

            if (!ports.Add(listener.Port))
            {
                yield return $"Duplicate listener port: {listener.Port}.";
            }

            if (string.IsNullOrWhiteSpace(listener.Prefixes?.FirstOrDefault()))
            {
                yield return "Listener prefixes cannot be null or empty.";
            }
        }
    }
}
