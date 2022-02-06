using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using LibVLCSharp.Shared;
using Screenbox.Controls;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal class NotificationService : INotificationService
    {
        public event EventHandler<NotificationRaisedEventArgs> NotificationRaised;

        public event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

        private string _progressTitle;

        public void RaiseNotification(NotificationLevel level, string title, string message = default, object content = default)
        {
            var eventArgs = new NotificationRaisedEventArgs
            {
                Level = level,
                Title = title,
                Message = message,
                Content = content
            };
            NotificationRaised?.Invoke(this, eventArgs);
        }

        public void RaiseError(string title, string message = default) => RaiseNotification(NotificationLevel.Error, title, message);

        public void RaiseWarning(string title, string message = default) => RaiseNotification(NotificationLevel.Warning, title, message);

        public void RaiseInfo(string title, string message = default) => RaiseNotification(NotificationLevel.Info, title, message);

        public void SetVLCDiaglogHandlers(LibVLC libVLC)
        {
            if (libVLC.DialogHandlersSet)
            {
                libVLC.UnsetDialogHandlers();
            }

            libVLC.SetDialogHandlers(DisplayErrorMessage, DisplayLoginDialog, DisplayQuestionDialog, DisplayProgress, UpdateProgress);
        }

        private Task DisplayErrorMessage(string title, string text)
        {
            return Task.Run(() => RaiseError(title, text));
        }

        private Task DisplayProgress(Dialog dialog, string title, string text, bool indeterminate, float position, string cancelText, CancellationToken token)
        {
            return Task.Run(() =>
            {
                if (token.IsCancellationRequested) return;
                _progressTitle = title;
                var eventArgs = new ProgressUpdatedEventArgs
                {
                    Title = title,
                    Text = text,
                    IsIndeterminate = indeterminate,
                    Value = position,
                };
                ProgressUpdated?.Invoke(this, eventArgs);
            });
        }

        private Task UpdateProgress(Dialog dialog, float position, string text) =>
            DisplayProgress(dialog, _progressTitle, text, false, position, null, CancellationToken.None);

        private async Task DisplayLoginDialog(Dialog dialog, string title, string text, string defaultUsername, bool askStore, CancellationToken token)
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

        private async Task DisplayQuestionDialog(Dialog dialog, string title, string text, DialogQuestionType type, string cancelText,
            string firstActionText, string secondActionText, CancellationToken token)
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
