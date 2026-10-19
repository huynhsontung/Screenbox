namespace Screenbox.Core.Services;

/// <summary>
/// Provides low-level, key-value storage for application settings.
/// </summary>
public interface ISettingsStorage
{
    /// <summary>
    /// Gets the setting value associated with the specified key, or the default value if not found.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <param name="defaultValue">The value to return if the setting has not been stored.</param>
    /// <returns>The stored setting value, or <paramref name="defaultValue"/>.</returns>
    T GetValue<T>(string key, T defaultValue);

    /// <summary>
    /// Sets the setting value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <param name="value">The value to store.</param>
    void SetValue<T>(string key, T value);
}
