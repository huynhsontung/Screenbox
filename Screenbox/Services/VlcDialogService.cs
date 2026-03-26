#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using Screenbox.Dialogs;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Services;

/// <summary>
/// Provides VLC dialog handler setup for media playback.
/// Handles VLC-specific dialogs by showing XAML dialogs and dispatching notifications via messaging.
/// </summary>
public sealed class VlcDialogService : IVlcDialogService
{
    private string? _progressTitle;

    /// <inheritdoc/>
    public void SetVlcDialogHandlers(LibVLC libVlc)
    {
        if (libVlc.DialogHandlersSet)
        {
            libVlc.UnsetDialogHandlers();
        }

        libVlc.SetDialogHandlers(DisplayErrorMessage, DisplayLoginDialog, DisplayQuestionDialog, DisplayProgress, UpdateProgress);
    }

    private Task DisplayErrorMessage(string? title, string? text)
    {
        return Task.Run(() =>
            WeakReferenceMessenger.Default.Send(new ErrorMessage(title ?? string.Empty, text ?? string.Empty)));
    }

    private Task DisplayProgress(Dialog dialog, string? title, string? text, bool indeterminate, float position, string? cancelText, CancellationToken token)
    {
        return Task.Run(() =>
        {
            if (!token.IsCancellationRequested)
                _progressTitle = title;
        }, token);
    }

    private Task UpdateProgress(Dialog dialog, float position, string? text) =>
        DisplayProgress(dialog, _progressTitle, text, false, position, null, CancellationToken.None);

    private async Task DisplayLoginDialog(Dialog dialog, string? title, string? text, string? defaultUsername, bool askStore, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;
        IVlcLoginDialog loginDialog = new VLCLoginDialog();
        loginDialog.Title = title ?? string.Empty;
        loginDialog.Text = text;
        loginDialog.Username = defaultUsername;
        loginDialog.AskStoreCredential = askStore;
        loginDialog.DefaultButton = ContentDialogButton.Primary;

        ContentDialogResult dialogResult;
        try
        {
            dialogResult = await loginDialog.ShowAsync();
        }
        catch (Exception)
        {
            // TODO: Handle this exception
            throw;
        }

        if (token.IsCancellationRequested) return;
        if (dialogResult == ContentDialogResult.Primary)
        {
            dialog.PostLogin(loginDialog.Username, loginDialog.Password, loginDialog.StoreCredential);
        }
        else
        {
            dialog.Dismiss();
        }
    }

    private async Task DisplayQuestionDialog(Dialog dialog, string? title, string? text, DialogQuestionType type, string? cancelText,
        string? firstActionText, string? secondActionText, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;
        var questionDialog = new ContentDialog
        {
            Title = title,
            Content = text,
            CloseButtonText = cancelText,
            PrimaryButtonText = firstActionText,
            SecondaryButtonText = secondActionText,
            DefaultButton = ContentDialogButton.None
        };

        ContentDialogResult dialogResult;
        try
        {
            dialogResult = await questionDialog.ShowAsync();
        }
        catch (Exception)
        {
            // TODO: Handle this exception
            throw;
        }

        if (token.IsCancellationRequested) return;
        switch (dialogResult)
        {
            case ContentDialogResult.Primary:
                dialog.PostAction(1);
                break;
            case ContentDialogResult.Secondary:
                dialog.PostAction(2);
                break;
            default:
                dialog.Dismiss();
                break;
        }
    }
}
