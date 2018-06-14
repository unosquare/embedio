namespace Unosquare.Labs.EmbedIO.Command
{
    using System.IO;
    using Unosquare.Swan;

    public static class Watcher
    {
        public static void WatchFiles(string path)
        {
            // Setup Websocket
            WebSocketWatcher.Setup();

            "Watching".Info(nameof(WatchFiles));
            var watcher = new FileSystemWatcher();
            watcher.Path = path;
            
            // Only watch text files.
            // watcher.Filter = "*.txt";

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            $"File: {e.FullPath} {e.ChangeType}".WriteLine();
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            $"File: {e.OldFullPath} renamed to {e.FullPath}".WriteLine();
        }
    }
}
