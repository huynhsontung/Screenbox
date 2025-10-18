#nullable enable

using LibVLCSharp.Shared;

namespace Screenbox.Core.Models
{
    public sealed class Renderer
    {
        public bool IsAvailable { get; private set; }

        public string Name { get; }

        public string Type { get; }

        public string? IconUri { get; }

        public bool CanRenderVideo { get; }

        public bool CanRenderAudio { get; }

        internal RendererItem? Target => IsAvailable ? _item : null;

        private readonly RendererItem _item;

        internal Renderer(RendererItem item)
        {
            _item = item;
            Name = item.Name;
            Type = item.Type;
            IconUri = item.IconUri;
            CanRenderVideo = item.CanRenderVideo;
            CanRenderAudio = item.CanRenderAudio;
            IsAvailable = true;
        }

        internal void Dispose()
        {
            IsAvailable = false;
            _item.Dispose();
        }

        public override string ToString()
        {
            return $"{Name}, {Type}";
        }
    }
}
