namespace Unosquare.Labs.EmbedIO.Constants
{
    /// <summary>
    /// Represents common responses Constants.
    /// </summary>
    internal static class Responses
    {
        internal const string ResponseBaseHtml = "<html><head></head><body>{0}</body></html>";

        /// <summary>
        /// Default Http Status 404 response output.
        /// </summary>
        internal const string Response404Html = "<html><head></head><body><h1>404 - Not Found</h1></body></html>";

        /// <summary>
        /// Default Status Http 405 response output.
        /// </summary>
        internal const string Response405Html = "<html><head></head><body><h1>405 - Method Not Allowed</h1></body></html>";
    }
}