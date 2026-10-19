using Windows.Foundation.Collections;
using Windows.Storage;

namespace Screenbox.Core.Services;

/// <summary>
/// Implements <see cref="ISettingsStorage"/> backed by UWP <see cref="ApplicationDataContainer.Values"/>.
/// </summary>
public sealed class SettingsStorage : ISettingsStorage
{
    private readonly IPropertySet _storage;

    public SettingsStorage() : this(ApplicationData.Current.LocalSettings.Values)
    {
    }

    public SettingsStorage(IPropertySet storage)
    {
        _storage = storage;
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        if (_storage.TryGetValue(key, out object? value) && value is T typedValue)
        {
            return typedValue;
        }

        return defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        _storage[key] = value;
    }
}
