namespace EmbedIO
{
    /// <summary>
    /// An event handler that is called whenever the state of a web server is changed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="WebServerStateChangedEventArgs"/> instance containing the event data.</param>
    public delegate void WebServerStateChangedEventHandler(object sender, WebServerStateChangedEventArgs e);
}