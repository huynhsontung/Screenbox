using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Base class for observable settings contexts backed by <see cref="ISettingsStorage"/> with an in-memory cache.
/// </summary>
public abstract partial class SettingsContextBase : ObservableRecipient
{
    private readonly Dictionary<string, object?> _cache = new();

    protected ISettingsStorage Storage { get; }

    protected SettingsContextBase(ISettingsStorage storage)
    {
        Storage = storage;
    }

    protected T Get<T>(string key, T defaultValue)
    {
        if (_cache.TryGetValue(key, out object? cached))
        {
            return (T)cached!;
        }

        T loaded = ReadFromStorage(key, defaultValue);
        _cache[key] = loaded;
        return loaded;
    }

    protected bool Set<T>(string key, T value, [CallerMemberName] string? propertyName = null)
    {
        if (_cache.TryGetValue(key, out object? cached))
        {
            T current = (T)cached!;
            if (EqualityComparer<T>.Default.Equals(current, value))
            {
                return false;
            }

            _cache[key] = value;
            WriteToStorage(key, value);
            OnPropertyChanged(propertyName);
            Messenger.Send(new PropertyChangedMessage<T>(this, propertyName, current, value));
            return true;
        }

        T previous = ReadFromStorage<T>(key, default!);
        _cache[key] = value;
        WriteToStorage(key, value);
        OnPropertyChanged(propertyName);
        Messenger.Send(new PropertyChangedMessage<T>(this, propertyName, previous, value));
        return true;
    }

    private T ReadFromStorage<T>(string key, T defaultValue)
    {
        if (typeof(T).IsEnum)
        {
            int defaultInt = Unsafe.As<T, int>(ref defaultValue);
            int raw = Storage.GetValue(key, defaultInt);
            return Unsafe.As<int, T>(ref raw);
        }

        return Storage.GetValue(key, defaultValue);
    }

    private void WriteToStorage<T>(string key, T value)
    {
        if (typeof(T).IsEnum)
        {
            Storage.SetValue(key, Unsafe.As<T, int>(ref value));
        }
        else
        {
            Storage.SetValue(key, value);
        }
    }
}
