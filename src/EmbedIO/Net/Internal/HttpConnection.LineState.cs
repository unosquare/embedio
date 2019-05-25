namespace EmbedIO.Net.Internal
{
    partial class HttpConnection
    {
        private enum LineState
        {
            None,
            Cr,
            Lf,
        }
    }
}