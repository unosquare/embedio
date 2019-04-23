namespace Unosquare.Labs.EmbedIO.Core
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    internal static class PathHelper
    {
        private static readonly Regex MultipleSlashRegex = new Regex("//+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly char[] InvalidLocalPathChars = GetInvalidLocalPathChars();

        // urlPath must be a valid URL path
        // (not null, not empty, starting with a slash.)
        public static string NormalizeUrlPath(string urlPath, bool isBasePath)
        {
            // Replace each run of multiple slashes with a single slash
            urlPath = MultipleSlashRegex.Replace(urlPath, "/");

            // The root path needs no further checking.
            var length = urlPath.Length;
            if (length == 1)
                return urlPath;

            // Base URL paths must end with a slash;
            // non-base URL paths must NOT end with a slash.
            // The final slash is irrelevant for the URL itself
            // (it has to map the same way with or without it)
            // but makes comparing and mapping URls a lot simpler.
            var finalPosition = length - 1;
            var endsWithSlash = urlPath[finalPosition] == '/';
            return isBasePath
                ? (endsWithSlash ? urlPath : urlPath + "/")
                : (endsWithSlash ? urlPath.Substring(0, finalPosition) : urlPath);
        }

        public static string EnsureValidUrlPath(string urlPath, bool isBasePath)
        {
            if (urlPath == null)
                throw new InvalidOperationException("URL path is null,");

            if (urlPath.Length == 0)
                throw new InvalidOperationException("URL path is empty.");

            if (urlPath[0] != '/')
                throw new InvalidOperationException($"URL path \"{urlPath}\"does not start with a slash.");

            return NormalizeUrlPath(urlPath, isBasePath);
        }

        public static string EnsureValidLocalPath(string localPath)
        {
            if (localPath == null)
                throw new InvalidOperationException("Local path is null.");

            if (localPath.Length == 0)
                throw new InvalidOperationException("Local path is empty.");

            if (string.IsNullOrWhiteSpace(localPath))
                throw new InvalidOperationException("Local path contains only white space.");

            if (localPath.IndexOfAny(InvalidLocalPathChars) >= 0)
                throw new InvalidOperationException($"Local path \"{localPath}\"contains one or more invalid characters.");

            return localPath;
        }

        private static char[] GetInvalidLocalPathChars()
        {
            var systemChars = Path.GetInvalidPathChars();
            var p = systemChars.Length;
            var result = new char[p + 2];
            Array.Copy(systemChars, result, p);
            result[p++] = '*';
            result[p] = '?';
            return result;
        }
    }
}