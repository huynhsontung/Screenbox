#nullable enable

using Sharpcaster.Models;

namespace Screenbox.Core.Models
{
    /// <summary>
    /// Represents a discovered Chromecast renderer (cast target) on the local network.
    /// </summary>
    public sealed class Renderer
    {
        /// <summary>Gets a value indicating whether this device is still reachable.</summary>
        public bool IsAvailable { get; private set; }

        /// <summary>Gets the display name of the Chromecast device.</summary>
        public string Name { get; }

        /// <summary>Gets the type identifier for the renderer (always "chromecast").</summary>
        public string Type => "chromecast";

        /// <summary>Gets an optional icon URI for the device; currently not provided by SharpCaster.</summary>
        public string? IconUri => null;

        /// <summary>Chromecast devices always support video rendering.</summary>
        public bool CanRenderVideo => true;

        /// <summary>Chromecast devices always support audio rendering.</summary>
        public bool CanRenderAudio => true;

        /// <summary>Gets the underlying SharpCaster receiver used to connect to this device.</summary>
        internal ChromecastReceiver Target { get; }

        internal Renderer(ChromecastReceiver receiver)
        {
            Target = receiver;
            Name = receiver.Name;
            IsAvailable = true;
        }

        /// <summary>Marks this renderer as no longer available (e.g., it left the network).</summary>
        internal void MarkUnavailable()
        {
            IsAvailable = false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Name}, {Type}";
        }
    }
}
