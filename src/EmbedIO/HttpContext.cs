namespace EmbedIO
{
    public static class HttpContext
    {
        /// <summary>
        /// The key used to store session information
        /// in the <seealso cref="IHttpContext.Items"/> dictionary.
        /// </summary>
        public static readonly object SessionKey = new object();
    }
}