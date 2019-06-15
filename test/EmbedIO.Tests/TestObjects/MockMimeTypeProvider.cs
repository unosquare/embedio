namespace EmbedIO.Tests.TestObjects
{
    public class MockMimeTypeProvider : IMimeTypeProvider
    {
        public string GetMimeType(string extension) => MimeTypes.Default;
    }
}