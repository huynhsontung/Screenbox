#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using LibVLCSharp.Shared;
using Screenbox.Controls;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal sealed class NotificationService : INotificationService
    {
        public event EventHandler<NotificationRaisedEventArgs>? NotificationRaised;

        public event EventHandler<ProgressUpdatedEventArgs>? ProgressUpdated;

        private string? _progressTitle;

        public void RaiseNotification(NotificationLevel level, string title, string message)
        {
            NotificationRaisedEventArgs eventArgs = new(level, title, message);
            NotificationRaised?.Invoke(this, eventArgs);
        }

        public void RaiseError(string title, string message) => RaiseNotification(NotificationLevel.Error, title, message);

        public void RaiseWarning(string title, string message) => RaiseNotification(NotificationLevel.Warning, title, message);

        public void RaiseInfo(string title, string message) => RaiseNotification(NotificationLevel.Info, title, message);

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
            return Task.Run(() => RaiseError(title ?? string.Empty, text ?? string.Empty));
        }

        private Task DisplayProgress(Dialog dialog, string? title, string? text, bool indeterminate, float position, string? cancelText, CancellationToken token)
        {
            return Task.Run(() =>
            {
                if (token.IsCancellationRequested) return;
                _progressTitle = title;
                var eventArgs = new ProgressUpdatedEventArgs(title, text, indeterminate, position);
                ProgressUpdated?.Invoke(this, eventArgs);
            }, token);
        }

        private Task UpdateProgress(Dialog dialog, float position, string? text) =>
            DisplayProgress(dialog, _progressTitle, text, false, position, null, CancellationToken.None);

        private async Task DisplayLoginDialog(Dialog dialog, string? title, string? text, string? defaultUsername, bool askStore, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            var loginDialog = new VLCLoginDialog
            {
                Title = title,
                Text = text,
                Username = defaultUsername,
                AskStoreCredential = askStore,
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult dialogResult;
            try
            {
                dialogResult = await loginDialog.ShowAsync();
            }
            catch (Exception)
            {
                // TODO: Handled this exception
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
                // TODO: Handled this exception
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
}
