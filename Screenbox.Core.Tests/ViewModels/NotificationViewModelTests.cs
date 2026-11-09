using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.ViewModels;
using TUnit.Core;

namespace Screenbox.Core.Tests.ViewModels;

[NotInParallel("NotificationViewModelTests")]
public class NotificationViewModelTests
{
    [Test]
    public async Task Receive_WhenNotOpen_DisplaysNotificationImmediately()
    {
        var vm = new NotificationViewModel();
        var message = new NotificationMessage(
            NotificationLevel.Info,
            NotificationKind.ResumePosition,
            title: "Test Title");

        try
        {
            vm.Receive(message);

            await Assert.That(vm.IsOpen).IsTrue();
            await Assert.That(vm.Kind).IsEqualTo(NotificationKind.ResumePosition);
            await Assert.That(vm.Title).IsEqualTo("Test Title");
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }

    [Test]
    public async Task Receive_WhenAlreadyOpen_QueuesAndDisplaysOnDismiss()
    {
        var vm = new NotificationViewModel();
        var message1 = new NotificationMessage(
            NotificationLevel.Info,
            NotificationKind.ResumePosition,
            title: "Resume Title");
        var message2 = new NotificationMessage(
            NotificationLevel.Info,
            NotificationKind.EnqueueFolderItems,
            title: "Folder Title");

        try
        {
            vm.Receive(message1);
            await Assert.That(vm.IsOpen).IsTrue();
            await Assert.That(vm.Kind).IsEqualTo(NotificationKind.ResumePosition);

            vm.Receive(message2);
            // Still displaying first message
            await Assert.That(vm.IsOpen).IsTrue();
            await Assert.That(vm.Kind).IsEqualTo(NotificationKind.ResumePosition);

            // Close first message
            vm.Receive(new CloseNotificationMessage());

            // Dequeues and shows second message
            await Assert.That(vm.IsOpen).IsTrue();
            await Assert.That(vm.Kind).IsEqualTo(NotificationKind.EnqueueFolderItems);
            await Assert.That(vm.Title).IsEqualTo("Folder Title");

            // Close second message
            vm.Receive(new CloseNotificationMessage());
            await Assert.That(vm.IsOpen).IsFalse();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }

    [Test]
    public async Task Receive_WhenErrorNotificationArrives_PreemptsActiveAndClearsQueue()
    {
        var vm = new NotificationViewModel();
        var message1 = new NotificationMessage(
            NotificationLevel.Info,
            NotificationKind.ResumePosition,
            title: "Resume Title");
        var message2 = new NotificationMessage(
            NotificationLevel.Info,
            NotificationKind.EnqueueFolderItems,
            title: "Folder Title");
        var errorMessage = new NotificationMessage(
            NotificationLevel.Error,
            NotificationKind.MediaLoadFailed,
            title: "Error Title");

        try
        {
            vm.Receive(message1);
            vm.Receive(message2);

            vm.Receive(errorMessage);

            await Assert.That(vm.IsOpen).IsTrue();
            await Assert.That(vm.Kind).IsEqualTo(NotificationKind.MediaLoadFailed);
            await Assert.That(vm.Severity).IsEqualTo(NotificationLevel.Error);

            // Dismissing error should not show previously queued item
            vm.Receive(new CloseNotificationMessage());
            await Assert.That(vm.IsOpen).IsFalse();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }

    [Test]
    public async Task Receive_WhenQueueCurrentItemChanges_PurgesStaleMediaPrompts()
    {
        var vm = new NotificationViewModel();
        var message1 = new NotificationMessage(
            NotificationLevel.Info,
            NotificationKind.ResumePosition,
            title: "Resume Title");
        var message2 = new NotificationMessage(
            NotificationLevel.Info,
            NotificationKind.EnqueueFolderItems,
            title: "Folder Title");

        try
        {
            vm.Receive(message1);
            vm.Receive(message2);

            // Media changes
            vm.Receive(new QueueCurrentItemChangedMessage(null));

            // Active prompt was dismissed and queued folder prompt was purged
            await Assert.That(vm.IsOpen).IsFalse();
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(vm);
        }
    }
}
