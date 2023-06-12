using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using Screenbox.Core.Messages;

namespace Screenbox.Controls;

public class VideoView : LibVLCSharp.Platforms.Windows.VideoView
{
    protected override void OnApplyTemplate()
    {
        try
        {
            base.OnApplyTemplate();
        }
        catch (VLCException e)
        {
            if (e.Message.StartsWith("Could not create Direct3D11 device"))
            {
                WeakReferenceMessenger.Default.Send(new CriticalErrorMessage(Strings.Resources.CriticalErrorDirect3D11NotAvailable));
            }
            else
            {
                throw;
            }
        }
    }
}