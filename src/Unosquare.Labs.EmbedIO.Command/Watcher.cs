namespace Unosquare.Labs.EmbedIO.Command
{
    using System.IO;
    using Swan;
    using System;
    using Swan.Abstractions;

    public class Watcher : SingletonBase<Watcher>
    {
        public event EventHandler<object> RefreshPage;

        public void WatchFiles(string path)
        {
            // Setup Websocket
            WebSocketWatcher.Setup();

            "Watching".Info(nameof(WatchFiles));
            var watcher = new FileSystemWatcher {Path = path};

            // Only watch text files.
            // watcher.Filter = "*.txt";

            // Add event handlers.
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e) => RefreshPage?.Invoke(null, null);

        private void OnRenamed(object source, RenamedEventArgs e) => RefreshPage?.Invoke(null, null);
    }
}
