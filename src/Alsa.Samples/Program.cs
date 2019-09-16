using System;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Media;

namespace Alsa.Samples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            SoundConnectionSettings settings = new SoundConnectionSettings();
            SoundDevice device = SoundDevice.Create(settings);

            await device.PlayAsync("/home/pi/1.wav", CancellationToken.None);
            //await device.ReccordAsync(10, "/home/pi/recording.wav", CancellationToken.None);

            //device.Volume = 10;
            //Console.WriteLine(device.Volume);

            Console.ReadKey();
        }
    }
}
