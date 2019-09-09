using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.Media
{
    public class SoundConnectionSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SoundConnectionSettings"/> class.
        /// </summary>
        /// <param name="busId">The bus ID the sound device is connected to.</param>
        public SoundConnectionSettings(int busId)
        {
            BusId = busId;
        }

        /// <summary>
        /// The bus ID the sound device is connected to.
        /// </summary>
        public int BusId { get; }
    }
}
