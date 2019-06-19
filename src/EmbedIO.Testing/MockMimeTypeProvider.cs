namespace EmbedIO.Testing
{
    public class MockMimeTypeProvider : IMimeTypeProvider
    {
        public string GetMimeType(string extension) => MimeType.Default;

        public bool TryDetermineCompression(string mimeType, out bool preferCompression)
        {
            preferCompression = default;
            return false;
        }
    }
}