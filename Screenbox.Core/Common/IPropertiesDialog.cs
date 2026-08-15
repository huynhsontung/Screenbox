using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Common;

public interface IPropertiesDialog : IDialog
{
    MediaViewModel? Media { get; set; }
}
