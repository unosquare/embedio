namespace EmbedIO.Testing
{
    public class MockMimeTypeProvider : IMimeTypeProvider
    {
        public string GetMimeType(string extension) => MimeType.Default;
    }
}