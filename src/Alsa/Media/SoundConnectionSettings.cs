using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Media
{
    public class SoundConnectionSettings
    {
        /// <summary>
        /// The playback device name of the sound device is connected to.
        /// </summary>
        public string PlaybackDeviceName { get; set; } = "default";

        public string RecordingDeviceName { get; set; } = "default";

        public uint RecordingSampleRate { get; set; } = 8000;

        public ushort RecordingChannels { get; set; } = 2;

        public ushort RecordingBitsPerSample { get; set; } = 16;
    }
}
