using System;
using System.Text;

namespace EmbedIO.Testing
{
    partial class MockFileProvider
    {
        private sealed class MockFile : MockDirectoryEntry
        {
            public MockFile(byte[] data)
            {
                Data = data ?? Array.Empty<byte>();
            }

            public MockFile(string text)
            {
                Data = text == null
                    ? Array.Empty<byte>()
                    : Encoding.UTF8.GetBytes(text);
            }

            public byte[] Data { get; private set; }

            public void SetData(byte[] data)
            {
                Data = data ?? Array.Empty<byte>();
                Touch();
            }

            public void SetData(string text)
            {
                Data = text == null
                    ? Array.Empty<byte>()
                    : Encoding.UTF8.GetBytes(text);
                Touch();
            }
        }
    }
}