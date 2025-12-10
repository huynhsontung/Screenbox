#nullable enable

using Screenbox.Core.Enums;
using Windows.UI.Core;

namespace Screenbox.Core.Contexts;

public sealed class WindowContext
{
    public CoreCursor? Cursor { get; set; }
    public WindowViewMode ViewMode { get; set; }
}
