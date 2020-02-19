namespace EmbedIO.Testing.Internal
{
    partial class TestMessageHandler
    {
        private enum ResponseHeaderType
        {
            // The header must be ignored
            None,

            // The header should be added to the Content property's Headers
            Content,

            // The header must be added to the response's Headers
            Response,
        }
    }
}