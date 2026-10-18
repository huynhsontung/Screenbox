using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Non-generic base class for observable settings contexts backed by <see cref="ISettingsStorage"/>.
/// </summary>
public abstract partial class SettingsContextBase : ObservableRecipient
{
    protected ISettingsStorage Storage { get; }

    protected SettingsContextBase(ISettingsStorage storage)
    {
        Storage = storage;
    }
}

/// <summary>
/// Base class for observable settings contexts backed by <see cref="ISettingsStorage"/> with an in-memory cache
/// and strongly-typed domain change notifications.
/// </summary>
/// <typeparam name="TSelf">The specific derived settings context type.</typeparam>
public abstract partial class SettingsContextBase<TSelf> : SettingsContextBase
    where TSelf : SettingsContextBase<TSelf>
{
    private readonly Dictionary<string, object?> _cache = new();

    protected SettingsContextBase(ISettingsStorage storage) : base(storage)
    {
    }

    protected T Get<T>(string key, T defaultValue)
    {
        if (_cache.TryGetValue(key, out object? cached) && cached is T current)
        {
            return current;
        }

        T loaded = ReadFromStorage(key, defaultValue);
        _cache[key] = loaded;
        return loaded;
    }

    protected bool Set<T>(string key, T value, [CallerMemberName] string propertyName = "")
    {
        if (_cache.TryGetValue(key, out object? cached) && cached is T current)
        {
            if (EqualityComparer<T>.Default.Equals(current, value))
            {
                return false;
            }

            _cache[key] = value;
            WriteToStorage(key, value);
            OnPropertyChanged(propertyName);
            Messenger.Send(new PropertyChangedMessage<T>(this, propertyName, current, value));
            Messenger.Send(new SettingChangedMessage<TSelf, T>((TSelf)this, propertyName, current, value));
            return true;
        }

        T previous = ReadFromStorage<T>(key, default!);
        _cache[key] = value;
        WriteToStorage(key, value);
        OnPropertyChanged(propertyName);
        Messenger.Send(new PropertyChangedMessage<T>(this, propertyName, previous, value));
        Messenger.Send(new SettingChangedMessage<TSelf, T>((TSelf)this, propertyName, previous, value));
        return true;
    }

    private T ReadFromStorage<T>(string key, T defaultValue)
    {
        if (typeof(T).IsEnum)
        {
            int defaultInt = defaultValue is not null ? (int)(object)defaultValue : 0;
            int raw = Storage.GetValue(key, defaultInt);
            return (T)(object)raw;
        }

        return Storage.GetValue(key, defaultValue);
    }

    private void WriteToStorage<T>(string key, T value)
    {
        if (typeof(T).IsEnum)
        {
            int raw = value is not null ? (int)(object)value : 0;
            Storage.SetValue(key, raw);
        }
        else
        {
            Storage.SetValue(key, value);
        }
    }
}
