#nullable enable


using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Common
{
    public interface IPropertiesDialog : IDialog
    {
        MediaViewModel? Media { get; set; }
    }
}
