namespace Unosquare.Labs.EmbedIO.Core
{
    using System;
    using System.IO;

    internal sealed class VirtualPath
    {
        public VirtualPath(string baseUrlPath, string baseLocalPath)
        {
            BaseUrlPath = PathHelper.EnsureValidUrlPath(baseUrlPath, true);
            try
            {
                BaseLocalPath = Path.GetFullPath(PathHelper.EnsureValidLocalPath(baseLocalPath));
            }
#pragma warning disable CA1031
            catch (Exception e)
            {
                throw new InvalidOperationException($"Cannot determine the full local path for \"{baseLocalPath}\".", e);
            }
#pragma warning restore CA1031
        }

        public string BaseUrlPath { get; }

        public string BaseLocalPath { get; }

        // Base paths are forced to end with a slash,
        // while requested paths are forced to NOT end with a slash.
        // Virtual path "/media/" can map "/media/file.jpg"
        // but it can also map "/media" (without the slash).

        internal bool CanMapUrlPath(string urlPath)
            => urlPath.StartsWith(BaseUrlPath, StringComparison.Ordinal)
            || (urlPath.Length == BaseUrlPath.Length - 1 && BaseUrlPath.StartsWith(urlPath, StringComparison.Ordinal));

        internal bool TryMapUrlPathLoLocalPath(string urlPath, out string localPath)
        {
            if (!CanMapUrlPath(urlPath))
            {
                localPath = null;
                return false;
            }

            // The only case where CanMapUrlPath returns true for a path shorter than BaseUrlPath
            // is urlPath == (BaseUrlPath minus the final slash).
            var relativeUrlPath = urlPath.Length < BaseUrlPath.Length
                ? string.Empty
                : urlPath.Substring(BaseUrlPath.Length);
            localPath = Path.Combine(BaseLocalPath, relativeUrlPath.Replace('/', Path.DirectorySeparatorChar));
            return true;
        }
    }
}