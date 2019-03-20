namespace Unosquare.Net
{
    using System.IO;
    using Labs.EmbedIO;
    using System.Threading.Tasks;
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

        public void AddPayload(MemoryStream data) => data.CopyTo(this, 1024);

        public async Task<MessageEventArgs> GetMessage(CompressionMethod compression)
        {
            var data = _fragmentsCompressed
                ? await this.CompressAsync(compression, System.IO.Compression.CompressionMode.Decompress).ConfigureAwait(false)
                : this;

            return new MessageEventArgs(_fragmentsOpcode, data.ToArray());
        }
    }
}
