#nullable enable

using Screenbox.Casting.Models;

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

        internal CastDevice? TargetDevice => IsAvailable ? _device : null;

        internal string Id => _device.Id;

        private readonly CastDevice _device;

        internal Renderer(CastDevice device)
        {
            _device = device;
            Name = device.Name;
            Type = device.Protocol.ToString();
            IconUri = device.IconUri;
            CanRenderVideo = device.CanRenderVideo;
            CanRenderAudio = device.CanRenderAudio;
            IsAvailable = true;
        }

        internal void Dispose()
        {
            IsAvailable = false;
        }

        public override string ToString()
        {
            return $"{Name}, {Type}";
        }
    }
}
