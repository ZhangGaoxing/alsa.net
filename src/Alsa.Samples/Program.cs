using System;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Media;

namespace Alsa.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            SoundConnectionSettings settings = new SoundConnectionSettings();
            using SoundDevice device = SoundDevice.Create(settings);

            device.Play("/home/pi/44.wav");

            Console.WriteLine("Play stop.");
        }
    }
}
