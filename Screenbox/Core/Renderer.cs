#nullable enable

using LibVLCSharp.Shared;

namespace Screenbox.Core
{
    public class Renderer
    {
        public bool IsAvailable { get; private set; }

        public string Name => _item.Name;
        
        public string Type => _item.Type;

        public string? IconUri => _item.IconUri;

        public bool CanRenderVideo => _item.CanRenderVideo;

        public bool CanRenderAudio => _item.CanRenderAudio;

        internal RendererItem? Target => IsAvailable ? _item : null;

        private readonly RendererItem _item;

        internal Renderer(RendererItem item)
        {
            _item = item;
            IsAvailable = true;
        }

        internal void Dispose()
        {
            IsAvailable = false;
            _item.Dispose();
        }
    }
}
