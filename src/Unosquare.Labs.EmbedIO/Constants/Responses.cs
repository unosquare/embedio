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

        /// <summary>
        /// Default Http Status 500 response output:
        /// 
        /// The first format argument takes the error message.
        /// The second format argument takes the stack trace.
        /// </summary>
        internal const string Response500HtmlFormat =
            "<html><head></head><body><h1>500 - Internal Server Error</h1><h2>Message</h2><pre>{0}</pre><h2>Stack Trace</h2><pre>\r\n{1}</pre></body></html>";

        internal const string HtmlContentType = "text/html";
    }
}