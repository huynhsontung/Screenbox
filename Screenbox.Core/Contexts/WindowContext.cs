#nullable enable

using Screenbox.Core.Enums;
using Windows.UI.Core;

namespace Screenbox.Core.Contexts;

internal sealed class WindowContext
{
    internal CoreCursor? Cursor { get; set; }
    internal WindowViewMode ViewMode { get; set; }
}
