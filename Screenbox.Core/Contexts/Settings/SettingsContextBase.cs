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

    protected bool Get(string key, bool defaultValue) => GetCore(key, defaultValue);

    protected bool Set(string key, bool value, [CallerMemberName] string propertyName = "") =>
        SetCore(key, value, propertyName);

    protected int Get(string key, int defaultValue) => GetCore(key, defaultValue);

    protected bool Set(string key, int value, [CallerMemberName] string propertyName = "") =>
        SetCore(key, value, propertyName);

    protected double Get(string key, double defaultValue) => GetCore(key, defaultValue);

    protected bool Set(string key, double value, [CallerMemberName] string propertyName = "") =>
        SetCore(key, value, propertyName);

    protected string Get(string key, string defaultValue) => GetCore(key, defaultValue);

    protected bool Set(string key, string value, [CallerMemberName] string propertyName = "") =>
        SetCore(key, value, propertyName);

    protected TEnum Get<TEnum>(string key, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        if (_cache.TryGetValue(key, out object? cached) && cached is TEnum current)
        {
            return current;
        }

        int defaultInt = (int)(object)defaultValue;
        int raw = Storage.GetValue(key, defaultInt);
        TEnum loaded = (TEnum)(object)raw;
        _cache[key] = loaded;
        return loaded;
    }

    protected bool Set<TEnum>(string key, TEnum value, [CallerMemberName] string propertyName = "")
        where TEnum : struct, Enum
    {
        int rawValue = (int)(object)value;

        if (_cache.TryGetValue(key, out object? cached) && cached is TEnum current)
        {
            if (EqualityComparer<TEnum>.Default.Equals(current, value))
            {
                return false;
            }

            _cache[key] = value;
            Storage.SetValue(key, rawValue);
            OnPropertyChanged(propertyName);
            Messenger.Send(new PropertyChangedMessage<TEnum>(this, propertyName, current, value));
            Messenger.Send(new SettingChangedMessage<TSelf, TEnum>((TSelf)this, propertyName, current, value));
            return true;
        }

        int previousRaw = Storage.GetValue(key, 0);
        TEnum previous = (TEnum)(object)previousRaw;
        _cache[key] = value;
        Storage.SetValue(key, rawValue);
        OnPropertyChanged(propertyName);
        Messenger.Send(new PropertyChangedMessage<TEnum>(this, propertyName, previous, value));
        Messenger.Send(new SettingChangedMessage<TSelf, TEnum>((TSelf)this, propertyName, previous, value));
        return true;
    }

    private T GetCore<T>(string key, T defaultValue)
    {
        if (_cache.TryGetValue(key, out object? cached) && cached is T current)
        {
            return current;
        }

        T loaded = Storage.GetValue(key, defaultValue);
        _cache[key] = loaded;
        return loaded;
    }

    private bool SetCore<T>(string key, T value, string propertyName)
    {
        if (_cache.TryGetValue(key, out object? cached) && cached is T current)
        {
            if (EqualityComparer<T>.Default.Equals(current, value))
            {
                return false;
            }

            _cache[key] = value;
            Storage.SetValue(key, value);
            OnPropertyChanged(propertyName);
            Messenger.Send(new PropertyChangedMessage<T>(this, propertyName, current, value));
            Messenger.Send(new SettingChangedMessage<TSelf, T>((TSelf)this, propertyName, current, value));
            return true;
        }

        T previous = Storage.GetValue<T>(key, default!);
        _cache[key] = value;
        Storage.SetValue(key, value);
        OnPropertyChanged(propertyName);
        Messenger.Send(new PropertyChangedMessage<T>(this, propertyName, previous, value));
        Messenger.Send(new SettingChangedMessage<TSelf, T>((TSelf)this, propertyName, previous, value));
        return true;
    }
}
