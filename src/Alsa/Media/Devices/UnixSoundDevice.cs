using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Iot.Device.Media
{
    /// <summary>
    /// Represents a communications channel to a sound device running on Unix.
    /// </summary>
    internal class UnixSoundDevice : SoundDevice
    {
        private IntPtr playbackPcm;
        private IntPtr recordingPcm;
        private IntPtr mixer;
        private IntPtr elem;
        private int errorNum;
        private static readonly object playbackInitializationLock = new object();
        private static readonly object recordingInitializationLock = new object();
        private static readonly object mixerInitializationLock = new object();

        /// <summary>
        /// The connection settings of the sound device.
        /// </summary>
        public override SoundConnectionSettings Settings { get; }

        /// <summary>
        /// The playback volume of the sound device.
        /// </summary>
        public override long PlaybackVolume { get => GetPlaybackVolume(); set => SetPlaybackVolume(value); }

        /// <summary>
        /// The recording volume of the sound device.
        /// </summary>
        public override long RecordingVolume { get => GetRecordingVolume(); set => SetRecordingVolume(value); }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnixSoundDevice"/> class that will use the specified settings to communicate with the sound device.
        /// </summary>
        /// <param name="settings">The connection settings of a sound device.</param>
        public UnixSoundDevice(SoundConnectionSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Play WAV file.
        /// </summary>
        /// <param name="wavPath">WAV file path.</param>
        /// <param name="token">A cancellation token that can be used to cancel the work.</param>
        public override async Task PlayAsync(string wavPath, CancellationToken token)
        {
            using FileStream fs = File.Open(wavPath, FileMode.Open);

            try
            {
                await Task.Run(() =>
                {
                    Play(fs);
                }, token);
            }
            catch (TaskCanceledException)
            {
                ClosePlaybackPcm();
            }
        }

        /// <summary>
        /// Play WAV file.
        /// </summary>
        /// <param name="wavStream">WAV stream.</param>
        /// <param name="token">A cancellation token that can be used to cancel the work.</param>
        public override async Task PlayAsync(Stream wavStream, CancellationToken token)
        {
            try
            {
                await Task.Run(() =>
                {
                    Play(wavStream);
                }, token);
            }
            catch (TaskCanceledException)
            {
                ClosePlaybackPcm();
            }
        }

        /// <summary>
        /// Sound recording.
        /// </summary>
        /// <param name="second">Recording duration(In seconds).</param>
        /// <param name="savePath">Recording save path.</param>
        /// <param name="token">A cancellation token that can be used to cancel the work.</param>
        public override async Task ReccordAsync(uint second, string savePath, CancellationToken token)
        {
            using FileStream fs = File.Open(savePath, FileMode.CreateNew);

            try
            {
                await Task.Run(() =>
                {
                    Record(fs, second);
                }, token);
            }
            catch (TaskCanceledException)
            {
                CloseRecordingPcm();
            }
        }

        /// <summary>
        /// Sound recording.
        /// </summary>
        /// <param name="second">Recording duration(In seconds).</param>
        /// <param name="saveStream">Recording save stream.</param>
        /// <param name="token">A cancellation token that can be used to cancel the work.</param>
        public override async Task ReccordAsync(uint second, Stream saveStream, CancellationToken token)
        {
            try
            {
                await Task.Run(() =>
                {
                    Record(saveStream, second);
                }, token);
            }
            catch (TaskCanceledException)
            {
                ClosePlaybackPcm();
            }
        }

        private void Play(Stream wavStream)
        {
            IntPtr @params = new IntPtr();
            int dir = 0;
            WavHeader header = GetWavHeader(wavStream);

            OpenPlaybackPcm();
            PcmInitialize(playbackPcm, header, ref @params, ref dir);
            WriteStream(wavStream, header, ref @params, ref dir);
            ClosePlaybackPcm();
        }

        private void Record(Stream saveStream, uint second)
        {
            IntPtr @params = new IntPtr();
            int dir = 0;
            WavHeader header = new WavHeader
            {
                ChunkId = new[] { 'R', 'I', 'F', 'F' },
                ChunkSize = second * Settings.RecordingSampleRate * Settings.RecordingBitsPerSample * Settings.RecordingChannels / 8 + 36,
                Format = new[] { 'W', 'A', 'V', 'E' },
                Subchunk1ID = new[] { 'f', 'm', 't', ' ' },
                Subchunk1Size = 16,
                AudioFormat = 1,
                NumChannels = Settings.RecordingChannels,
                SampleRate = Settings.RecordingSampleRate,
                ByteRate = Settings.RecordingSampleRate * Settings.RecordingBitsPerSample * Settings.RecordingChannels / 8,
                BlockAlign = (ushort)(Settings.RecordingBitsPerSample * Settings.RecordingChannels / 8),
                BitsPerSample = Settings.RecordingBitsPerSample,
                Subchunk2Id = new[] { 'd', 'a', 't', 'a' },
                Subchunk2Size = second * Settings.RecordingSampleRate * Settings.RecordingBitsPerSample * Settings.RecordingChannels / 8
            };

            SetWavHeader(saveStream, header);

            OpenRecordingPcm();
            PcmInitialize(recordingPcm, header, ref @params, ref dir);
            ReadStream(saveStream, header, ref @params, ref dir);
            CloseRecordingPcm();
        }

        private void SetWavHeader(Stream wavStream, WavHeader header)
        {
            Span<byte> writeBuffer2 = stackalloc byte[2];
            Span<byte> writeBuffer4 = stackalloc byte[4];

            wavStream.Position = 0;

            try
            {
                Encoding.ASCII.GetBytes(header.ChunkId, writeBuffer4);
                wavStream.Write(writeBuffer4);

                BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer4, header.ChunkSize);
                wavStream.Write(writeBuffer4);

                Encoding.ASCII.GetBytes(header.Format, writeBuffer4);
                wavStream.Write(writeBuffer4);

                Encoding.ASCII.GetBytes(header.Subchunk1ID, writeBuffer4);
                wavStream.Write(writeBuffer4);

                BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer4, header.Subchunk1Size);
                wavStream.Write(writeBuffer4);

                BinaryPrimitives.WriteUInt16LittleEndian(writeBuffer2, header.AudioFormat);
                wavStream.Write(writeBuffer2);

                BinaryPrimitives.WriteUInt16LittleEndian(writeBuffer2, header.NumChannels);
                wavStream.Write(writeBuffer2);

                BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer4, header.SampleRate);
                wavStream.Write(writeBuffer4);

                BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer4, header.ByteRate);
                wavStream.Write(writeBuffer4);

                BinaryPrimitives.WriteUInt16LittleEndian(writeBuffer2, header.BlockAlign);
                wavStream.Write(writeBuffer2);

                BinaryPrimitives.WriteUInt16LittleEndian(writeBuffer2, header.BitsPerSample);
                wavStream.Write(writeBuffer2);

                Encoding.ASCII.GetBytes(header.Subchunk2Id, writeBuffer4);
                wavStream.Write(writeBuffer4);

                BinaryPrimitives.WriteUInt32LittleEndian(writeBuffer4, header.Subchunk2Size);
                wavStream.Write(writeBuffer4);
            }
            catch
            {
                throw new Exception("Write WAV header error.");
            }
        }

        private WavHeader GetWavHeader(Stream wavStream)
        {
            Span<byte> readBuffer2 = stackalloc byte[2];
            Span<byte> readBuffer4 = stackalloc byte[4];

            wavStream.Position = 0;

            WavHeader header = new WavHeader();

            try
            {
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
            }
            catch
            {
                throw new Exception("Non-standard WAV file.");
            }

            return header;
        }

        private unsafe void WriteStream(Stream wavStream, WavHeader header, ref IntPtr @params, ref int dir)
        {
            ulong frames, bufferSize;

            fixed (int* dirP = &dir)
            {
                errorNum = Interop.snd_pcm_hw_params_get_period_size(@params, &frames, dirP);
                ThrowErrorMessage("Can not get period size.");
            }

            bufferSize = frames * header.BlockAlign;
            // In Interop, the frames is defined as ulong. But actucally, the value of bufferSize won't be too big.
            byte[] readBuffer = new byte[(int)bufferSize];
            // Jump wav header.
            wavStream.Position = 44;

            fixed (byte* buffer = readBuffer)
            {
                while (wavStream.Read(readBuffer) != 0)
                {
                    errorNum = Interop.snd_pcm_writei(playbackPcm, (IntPtr)buffer, frames);
                    ThrowErrorMessage("Can not write buffer to the device.");
                }
            }
        }

        private unsafe void ReadStream(Stream saveStream, WavHeader header, ref IntPtr @params, ref int dir)
        {
            int errorNum;
            ulong frames, bufferSize;

            fixed (int* dirP = &dir)
            {
                errorNum = Interop.snd_pcm_hw_params_get_period_size(@params, &frames, dirP);
                ThrowErrorMessage("Can not get period size.");
            }

            bufferSize = frames * header.BlockAlign;
            byte[] readBuffer = new byte[(int)bufferSize];

            fixed (byte* buffer = readBuffer)
            {
                for (int i = 0; i < (int)(header.Subchunk2Size / bufferSize); i++)
                {
                    errorNum = Interop.snd_pcm_readi(recordingPcm, (IntPtr)buffer, frames);
                    ThrowErrorMessage("Can not read buffer from the device.");

                    saveStream.Write(readBuffer);
                    saveStream.Flush();
                }
            }
        }

        private unsafe void PcmInitialize(IntPtr pcm, WavHeader header, ref IntPtr @params, ref int dir)
        {
            errorNum = Interop.snd_pcm_hw_params_malloc(ref @params);
            ThrowErrorMessage("Can not allocate parameters object.");

            errorNum = Interop.snd_pcm_hw_params_any(pcm, @params);
            ThrowErrorMessage("Can not fill parameters object.");

            errorNum = Interop.snd_pcm_hw_params_set_access(pcm, @params, snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED);
            ThrowErrorMessage("Can not set access mode.");

            errorNum = (int)(header.BitsPerSample / 8) switch
            {
                1 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_U8),
                2 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S16_LE),
                3 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S24_LE),
                _ => throw new Exception("Bits per sample error."),
            };
            ThrowErrorMessage("Can not set format.");

            errorNum = Interop.snd_pcm_hw_params_set_channels(pcm, @params, header.NumChannels);
            ThrowErrorMessage("Can not set channel.");

            uint val = header.SampleRate;
            fixed (int* dirP = &dir)
            {
                errorNum = Interop.snd_pcm_hw_params_set_rate_near(pcm, @params, &val, dirP);
                ThrowErrorMessage("Can not set rate.");
            }

            errorNum = Interop.snd_pcm_hw_params(pcm, @params);
            ThrowErrorMessage("Can not set hardware parameters.");
        }

        private unsafe void SetPlaybackVolume(long volume)
        {
            OpenMixer();

            errorNum = Interop.snd_mixer_selem_set_playback_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, volume);
            errorNum = Interop.snd_mixer_selem_set_playback_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, volume);
            ThrowErrorMessage("Set playback volume error.");

            CloseMixer();
        }

        private unsafe long GetPlaybackVolume()
        {
            long volumeLeft, volumeRight;

            OpenMixer();

            errorNum = Interop.snd_mixer_selem_get_playback_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, &volumeLeft);
            errorNum = Interop.snd_mixer_selem_get_playback_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, &volumeRight);
            ThrowErrorMessage("Get playback volume error.");

            CloseMixer();

            return (volumeLeft + volumeRight) / 2;
        }

        private unsafe void SetRecordingVolume(long volume)
        {
            OpenMixer();

            errorNum = Interop.snd_mixer_selem_set_capture_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, volume);
            errorNum = Interop.snd_mixer_selem_set_capture_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, volume);
            ThrowErrorMessage("Set recording volume error.");

            CloseMixer();
        }

        private unsafe long GetRecordingVolume()
        {
            long volumeLeft, volumeRight;

            OpenMixer();

            errorNum = Interop.snd_mixer_selem_get_capture_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_LEFT, &volumeLeft);
            errorNum = Interop.snd_mixer_selem_get_capture_volume(elem, snd_mixer_selem_channel_id.SND_MIXER_SCHN_FRONT_RIGHT, &volumeRight);
            ThrowErrorMessage("Get recording volume error.");

            CloseMixer();

            return (volumeLeft + volumeRight) / 2;
        }

        private void OpenPlaybackPcm()
        {
            if (playbackPcm != default)
            {
                return;
            }

            lock (playbackInitializationLock)
            {
                errorNum = Interop.snd_pcm_open(ref playbackPcm, Settings.PlaybackDeviceName, snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, 0);
                ThrowErrorMessage("Can not open playback device.");
            }
        }

        private void ClosePlaybackPcm()
        {
            if (playbackPcm != default)
            {
                errorNum = Interop.snd_pcm_drop(playbackPcm);
                ThrowErrorMessage("Drop playback device error.");

                errorNum = Interop.snd_pcm_close(playbackPcm);
                ThrowErrorMessage("Close playback device error.");

                playbackPcm = default;
            }
        }

        private void OpenRecordingPcm()
        {
            if (recordingPcm != default)
            {
                return;
            }

            lock (recordingInitializationLock)
            {
                errorNum = Interop.snd_pcm_open(ref recordingPcm, Settings.RecordingDeviceName, snd_pcm_stream_t.SND_PCM_STREAM_CAPTURE, 0);
                ThrowErrorMessage("Can not open recording device.");
            }
        }

        private void CloseRecordingPcm()
        {
            if (recordingPcm != default)
            {
                errorNum = Interop.snd_pcm_drop(recordingPcm);
                ThrowErrorMessage("Drop recording device error.");

                errorNum = Interop.snd_pcm_close(recordingPcm);
                ThrowErrorMessage("Close recording device error.");

                recordingPcm = default;
            }
        }

        private void OpenMixer()
        {
            if (mixer != default)
            {
                return;
            }

            lock (mixerInitializationLock)
            {
                errorNum = Interop.snd_mixer_open(ref mixer, 0);
                ThrowErrorMessage("Can not open sound device mixer.");

                errorNum = Interop.snd_mixer_attach(mixer, Settings.MixerDeviceName);
                ThrowErrorMessage("Can not attach sound device mixer.");

                errorNum = Interop.snd_mixer_selem_register(mixer, IntPtr.Zero, IntPtr.Zero);
                ThrowErrorMessage("Can not register sound device mixer.");

                errorNum = Interop.snd_mixer_load(mixer);
                ThrowErrorMessage("Can not load sound device mixer.");

                elem = Interop.snd_mixer_first_elem(mixer);
            }
        }

        private void CloseMixer()
        {
            if (mixer != default)
            {
                errorNum = Interop.snd_mixer_close(mixer);
                ThrowErrorMessage("Close sound device mixer error.");

                mixer = default;
                elem = default;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void ThrowErrorMessage(string message)
        {
            if (errorNum < 0)
            {
                string errorMsg = Marshal.PtrToStringAnsi(Interop.snd_strerror(errorNum));
                throw new Exception($"{message}\nError {errorNum}. {errorMsg}.");
            }
        }
    }
}
