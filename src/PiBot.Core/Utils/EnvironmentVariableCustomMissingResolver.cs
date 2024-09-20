using ConfigurationPlaceholders;
using Microsoft.Extensions.Configuration;

namespace PiBot.Core.Utils;

/// <summary>
///     Resolves placeholders from environment variables.
///     Searches with this priority:
///     1.  EnvironmentVariableTarget.Process
///     2.  EnvironmentVariableTarget.User
///     3.  EnvironmentVariableTarget.Machine
/// </summary>
public sealed class EnvironmentVariableCustomMissingResolver : IPlaceholderResolver
{
    #region Implementation of IPlaceholderResolver

    /// <summary>
    ///     Gets the value to use instead of the placeholder with the given key.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <param name="key">PPlaceholder key.</param>
    /// <param name="value">Value to use; null if value was not found.</param>
    /// <returns>True if a matching value was found; otherwise, false.</returns>
    public bool GetValue(IConfiguration configuration, string key, out string? value)
    {
        var split = key.Split("?");
        key = split[0];

        value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
        if (value is not null)
            return true;

        value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User);
        if (value is not null)
            return true;

        value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Machine);
        if (value is not null)
            return true;

        if (split.Length > 1 && !string.IsNullOrEmpty(split[1]))
        {
            value = split[1];
            return true;
        }

        return false;
    }

    #endregion
}