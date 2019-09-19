using System;
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

            Task.Run(() =>
            {
                device.Play("/home/pi/44.wav");
            });

            while (true)
            {
                if (Console.ReadKey() != null)
                {
                    device.PlaybackMute = !device.PlaybackMute;
                }
            }

            //Console.WriteLine("Play stop.");
        }
    }
}
