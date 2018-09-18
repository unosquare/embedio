namespace Unosquare.Net
{
    using System.IO;
    using Labs.EmbedIO;
    using Labs.EmbedIO.Constants;

    internal class FragmentBuffer : MemoryStream
    {
        private readonly bool _fragmentsCompressed;
        private readonly Opcode _fragmentsOpcode;

        public FragmentBuffer(Opcode frameOpcode, bool frameIsCompressed)
        {
            _fragmentsOpcode = frameOpcode;
            _fragmentsCompressed = frameIsCompressed;
        }

        public void AddPayload(byte[] data)
        {
            using (var input = new MemoryStream(data))
                input.CopyTo(this, 1024);
        }

        public MessageEventArgs GetMessage(CompressionMethod compression)
        {
            var data = _fragmentsCompressed
                ? this.Compress(compression, System.IO.Compression.CompressionMode.Decompress)
                : this;

            return new MessageEventArgs(_fragmentsOpcode, data.ToArray());
        }
    }
}
