#nullable enable

using Screenbox.Core.Enums;
using Windows.UI.Core;

namespace Screenbox.Core.Contexts;

internal sealed class WindowState
{
    internal CoreCursor? Cursor { get; set; }
    internal WindowViewMode ViewMode { get; set; }
}
