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

        internal bool CanMapUrlPath(string urlPath) => urlPath.StartsWith(BaseUrlPath, StringComparison.Ordinal);

        internal bool TryMapUrlPathLoLocalPath(string urlPath, out string localPath)
        {
            if (!CanMapUrlPath(urlPath))
            {
                localPath = null;
                return false;
            }

            var relativeUrlPath = urlPath.Substring(BaseUrlPath.Length);
            localPath = Path.Combine(BaseLocalPath, relativeUrlPath.Replace('/', Path.DirectorySeparatorChar));
            return true;
        }
    }
}