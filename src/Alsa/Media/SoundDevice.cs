using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Media
{
    public abstract partial class SoundDevice : IDisposable
    {
        /// <summary>
        /// Create a communications channel to a sound device running on Unix.
        /// </summary>
        /// <param name="settings">The connection settings of a sound device.</param>
        /// <returns>A communications channel to a sound device running on Unix.</returns>
        public static SoundDevice Create(SoundConnectionSettings settings) => new UnixSoundDevice(settings);

        /// <summary>
        /// Path to sound resources located on the platform.
        /// </summary>
        public abstract string DevicePath { get; set; }

        /// <summary>
        /// The connection settings of the sound device.
        /// </summary>
        public abstract SoundConnectionSettings Settings { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }
}
