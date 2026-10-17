using Screenbox.Core.Contexts;

namespace Screenbox.Core.Messages;

/// <summary>
/// Message broadcast via <see cref="CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger"/>
/// when a setting property value changes within a specific domain settings context.
/// </summary>
/// <typeparam name="TContext">The specific settings context type.</typeparam>
/// <typeparam name="TValue">The type of the setting property value.</typeparam>
/// <param name="Context">The settings context that owns the setting.</param>
/// <param name="PropertyName">The name of the changed property.</param>
/// <param name="OldValue">The previous value of the setting.</param>
/// <param name="NewValue">The new value of the setting.</param>
public sealed record SettingChangedMessage<TContext, TValue>(
    TContext Context,
    string? PropertyName,
    TValue OldValue,
    TValue NewValue)
    where TContext : SettingsContextBase;
