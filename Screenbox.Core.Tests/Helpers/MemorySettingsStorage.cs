using System.Collections.Generic;
using Screenbox.Core.Services;

namespace Screenbox.Core.Tests.Helpers;

public class MemorySettingsStorage : ISettingsStorage
{
    private readonly Dictionary<string, object?> _values = new();

    public T GetValue<T>(string key, T defaultValue)
    {
        if (_values.TryGetValue(key, out object? value) && value is T typedValue)
        {
            return typedValue;
        }

        return defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        _values[key] = value;
    }

    public bool ContainsKey(string key) => _values.ContainsKey(key);
}
