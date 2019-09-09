using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Iot.Device.Media
{
    public class UnixSoundDevice : SoundDevice
    {
        private IntPtr pcm;
        private const string DefaultDevicePath = "/dev/snd";
        private const int BufferCount = 4;
        private int _deviceFileDescriptor = -1;
        private static readonly object s_initializationLock = new object();

        public override string DevicePath { get; set; }

        public override SoundConnectionSettings Settings { get; }

        public UnixSoundDevice(SoundConnectionSettings settings)
        {
            Settings = settings;
            DevicePath = DefaultDevicePath;
        }

        private WavHeader GetWavHeader(Stream wavStream)
        {
            Span<byte> readBuffer2 = stackalloc byte[2];
            Span<byte> readBuffer4 = stackalloc byte[4];

            wavStream.Position = 0;

            WavHeader header = new WavHeader();

            wavStream.Read(readBuffer4);
            header.ChunkId = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

            wavStream.Read(readBuffer4);
            header.ChunkSize = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

            wavStream.Read(readBuffer4);
            header.Format = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

            wavStream.Read(readBuffer4);
            header.Subchunk1ID = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

            wavStream.Read(readBuffer4);
            header.Subchunk1Size = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

            wavStream.Read(readBuffer2);
            header.AudioFormat = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

            wavStream.Read(readBuffer2);
            header.NumChannels = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

            wavStream.Read(readBuffer4);
            header.SampleRate = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

            wavStream.Read(readBuffer4);
            header.ByteRate = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

            wavStream.Read(readBuffer2);
            header.BlockAlign = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

            wavStream.Read(readBuffer2);
            header.BitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

            wavStream.Read(readBuffer4);
            header.Subchunk2Id = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

            wavStream.Read(readBuffer4);
            header.Subchunk2Size = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

            return header;
        }

        public unsafe void PlayInitialize(Stream wavStream)
        {
            var header = GetWavHeader(wavStream);

            IntPtr @params = new IntPtr();
            ulong frames;
            uint val;
            int dir = 0;

            int error = Interop.snd_pcm_open(ref pcm, "default", snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, 0);
            if (error < 0)
            {
                Console.WriteLine(Marshal.GetLastWin32Error());
            }
            error = Interop.snd_pcm_hw_params_malloc(ref @params);
            if (error < 0)
            {
                Console.WriteLine(Marshal.GetLastWin32Error());
            }
            error = Interop.snd_pcm_hw_params_any(pcm, @params);
            if (error < 0)
            {
                Console.WriteLine(Marshal.GetLastWin32Error());
            }
            error = Interop.snd_pcm_hw_params_set_access(pcm, @params, snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED);
            if (error < 0)
            {
                Console.WriteLine(Marshal.GetLastWin32Error());
            }

            switch (header.BitsPerSample / 8)
            {
                case 1:
                    Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_U8);
                    break;
                case 2:
                    Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S16_LE);
                    break;
                case 3:
                    Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S24_LE);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Interop.snd_pcm_hw_params_set_channels(pcm, @params, header.NumChannels);
            Interop.snd_pcm_hw_params_set_rate_near(pcm, @params, &val, &dir);
            Interop.snd_pcm_hw_params(pcm, @params);
            Interop.snd_pcm_hw_params_get_period_size(@params, &frames, &dir);

            Console.WriteLine($"{frames}  {dir}");

            Interop.snd_pcm_drain(pcm);
            Interop.snd_pcm_close(pcm);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
