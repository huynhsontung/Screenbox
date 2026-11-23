using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents a live shortcut to a folder or playlist file (e.g. M3U/M3U8) origin in Recent Media.
/// </summary>
public sealed partial class MediaCollectionViewModel : ObservableRecipient,
    IRecentItem,
    IRecipient<QueuePlaybackStateChangedMessage>
{
    public string Name { get; }

    public string SourcePath { get; }

    public string? Token { get; set; }

    public bool IsPlaylistFile { get; }

    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsQueueActive { get; set; }

    private readonly IQueueService? _queueService;

    public MediaCollectionViewModel(
        string name,
        string sourcePath,
        IQueueService? queueService,
        string? token = null,
        IMessenger? messenger = null)
        : base(messenger ?? WeakReferenceMessenger.Default)
    {
        Name = name;
        SourcePath = sourcePath;
        Token = token;
        _queueService = queueService;
        IsPlaylistFile = IsPlaylistOriginPath(sourcePath);

        Messenger.Register<QueuePlaybackStateChangedMessage>(this);

        var request = new QueuePlaybackStateRequestMessage();
        Messenger.Send(request);
        if (request.HasReceivedResponse)
        {
            UpdatePlaybackState(request.Response);
        }
    }

    public void Receive(QueuePlaybackStateChangedMessage message)
    {
        UpdatePlaybackState(message.State);
    }

    private void UpdatePlaybackState(QueuePlaybackState state)
    {
        bool isActive = state.SessionId != Guid.Empty &&
                        state.CurrentItem is not null &&
                        (state.Origin?.Matches(RecentItemKind.MediaCollection, SourcePath) ?? false);
        IsQueueActive = isActive;
        IsPlaying = isActive && state.IsPlaying;
    }

    [RelayCommand]
    public async Task PlayAsync()
    {
        if (IsQueueActive)
        {
            Messenger.Send(new TogglePlayPauseMessage(false));
            return;
        }

        QueueSession? session = await ResolveLiveSessionAsync();
        if (session is { IsEmpty: false })
        {
            session.Origin = QueueOrigin.ForMediaCollection(Name, SourcePath, Token);
            Messenger.Send(new SetQueueMessage(session, shouldPlay: true));
        }
    }

    public async Task<IReadOnlyList<MediaViewModel>> GetPlayableItemsAsync()
    {
        QueueSession? session = await ResolveLiveSessionAsync();
        return session?.Items ?? (IReadOnlyList<MediaViewModel>)Array.Empty<MediaViewModel>();
    }

    private async Task<QueueSession?> ResolveLiveSessionAsync()
    {
        if (_queueService is null || string.IsNullOrEmpty(SourcePath))
            return null;

        try
        {
            if (IsPlaylistFile)
            {
                StorageFile? file = await TryGetFileAsync();
                if (file is not null)
                {
                    return await _queueService.ParseQueueAsync(file);
                }

                if (Uri.TryCreate(SourcePath, UriKind.Absolute, out Uri? uri) && !uri.IsFile)
                {
                    return await _queueService.ParseQueueAsync(uri);
                }

                return null;
            }

            StorageFolder? folder = await TryGetFolderAsync();
            return folder is not null
                ? await _queueService.TryCreateQueueAsync(new IStorageItem[] { folder })
                : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<StorageFile?> TryGetFileAsync() =>
        await FilesHelpers.TryGetFileFromTokenAsync(Token) ??
        await FilesHelpers.TryGetFileFromPathAsync(SourcePath);

    private async Task<StorageFolder?> TryGetFolderAsync() =>
        await FilesHelpers.TryGetFolderFromTokenAsync(Token) ??
        await FilesHelpers.TryGetFolderFromPathAsync(SourcePath);

    private static bool IsPlaylistOriginPath(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath))
            return false;

        if (Uri.TryCreate(sourcePath, UriKind.Absolute, out Uri? uri) && !uri.IsFile)
            return true;

        try
        {
            return FilesHelpers.IsSupportedPlaylist(Path.GetExtension(sourcePath));
        }
        catch
        {
            return false;
        }
    }
}
