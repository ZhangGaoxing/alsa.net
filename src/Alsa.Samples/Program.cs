using System;
using System.IO;
using Iot.Device.Media;

namespace Alsa.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            SoundConnectionSettings settings = new SoundConnectionSettings(1);
            UnixSoundDevice device = new UnixSoundDevice(settings);

            using FileStream fs = File.Open("/home/pi/1.wav", FileMode.Open);
            device.Play(fs);

            Console.ReadKey();
        }
    }
}
