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

        public override unsafe void Play(Stream wavStream)
        {
            IntPtr @params = new IntPtr();
            ulong frames, bufferSize;
            uint val = 0;
            int dir = 0;
            WavHeader header = GetWavHeader(wavStream);

            Open();
            PlayInitialize(header, ref @params, ref val, ref dir);

            Interop.snd_pcm_hw_params_get_period_size(@params, &frames, &dir);

            bufferSize = frames * header.BlockAlign;

            Close();
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

        private unsafe void PlayInitialize(WavHeader header, ref IntPtr @params, ref uint val, ref int dir)
        {
            if (Interop.snd_pcm_hw_params_malloc(ref @params) < 0)
            {
                throw new Exception($"Error {Marshal.GetLastWin32Error()}. Can not allocate parameters object.");
            }

            if (Interop.snd_pcm_hw_params_any(pcm, @params) < 0)
            {
                throw new Exception($"Error {Marshal.GetLastWin32Error()}. Can not fill parameters object.");
            }

            if (Interop.snd_pcm_hw_params_set_access(pcm, @params, snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED) < 0)
            {
                throw new Exception($"Error {Marshal.GetLastWin32Error()}. Can not set access mode.");
            }

            int error = (int)(header.BitsPerSample / 8) switch
            {
                1 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_U8),
                2 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S16_LE),
                3 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S24_LE),
                _ => throw new Exception("Bits per sample error."),
            };

            if (error < 0)
            {
                throw new Exception($"Error {Marshal.GetLastWin32Error()}. Can not set format.");
            }

            if (Interop.snd_pcm_hw_params_set_channels(pcm, @params, header.NumChannels) < 0)
            {
                throw new Exception($"Error {Marshal.GetLastWin32Error()}. Can not set channel.");
            }

            fixed (uint* valP = &val)
            {
                fixed (int* dirP = &dir)
                {
                    if (Interop.snd_pcm_hw_params_set_rate_near(pcm, @params, valP, dirP) < 0)
                    {
                        throw new Exception($"Error {Marshal.GetLastWin32Error()}. Can not set rate.");
                    }
                }
            }

            if (Interop.snd_pcm_hw_params(pcm, @params) < 0)
            {
                throw new Exception($"Error {Marshal.GetLastWin32Error()}. Can not set hardware parameters.");
            }
        }

        private void Open()
        {
            if (pcm != default)
            {
                return;
            }

            if (Interop.snd_pcm_open(ref pcm, "default", snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, 0) < 0)
            {
                throw new Exception($"Error {Marshal.GetLastWin32Error()}. Can not open sound device.");
            }
        }

        private void Close()
        {
            if (pcm != default)
            {
                if (Interop.snd_pcm_drain(pcm) < 0)
                {
                    throw new Exception($"Error {Marshal.GetLastWin32Error()}. Drain sound device error.");
                }

                if (Interop.snd_pcm_close(pcm) < 0)
                {
                    throw new Exception($"Error {Marshal.GetLastWin32Error()}. Close sound device error.");
                }

                pcm = default;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
