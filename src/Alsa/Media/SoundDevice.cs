using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        /// The connection settings of the sound device.
        /// </summary>
        public abstract SoundConnectionSettings Settings { get; }

        /// <summary>
        /// Play WAV file.
        /// </summary>
        /// <param name="wavPath">WAV file path.</param>
        /// <param name="token">A cancellation token that can be used to cancel the work.</param>
        public abstract Task PlayAsync(string wavPath, CancellationToken token);

        /// <summary>
        /// Play WAV file.
        /// </summary>
        /// <param name="wavStream">WAV stream.</param>
        /// <param name="token">A cancellation token that can be used to cancel the work.</param>
        public abstract Task PlayAsync(Stream wavStream, CancellationToken token);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }
}
