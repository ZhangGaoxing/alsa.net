namespace Iot.Device.Media
{
    internal struct WavHeader
    {
        public char[] ChunkId { get; set; }

        public uint ChunkSize { get; set; }

        public char[] Format { get; set; }

        public char[] Subchunk1ID { get; set; }

        public uint Subchunk1Size { get; set; }

        public ushort AudioFormat { get; set; }

        public ushort NumChannels { get; set; }

        public uint SampleRate { get; set; }

        public uint ByteRate { get; set; }

        public ushort BlockAlign { get; set; }

        public ushort BitsPerSample { get; set; }

        public char[] Subchunk2Id { get; set; }

        public uint Subchunk2Size { get; set; }
    }
}
