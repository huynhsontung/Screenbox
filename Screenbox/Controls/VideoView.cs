using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;

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
            WeakReferenceMessenger.Default.Send(new CriticalErrorMessage(Strings.Resources.CriticalErrorDirect3D11NotAvailable));
            LogService.Log(e);
        }
    }
}