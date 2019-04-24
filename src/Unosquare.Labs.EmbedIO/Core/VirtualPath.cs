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

            // Disable CA1031 as there's little we can do if IsPathRooted or GetFullPath fails.
#pragma warning disable CA1031
            try
            {
                // Bail out early if the path is a rooted path,
                // as Path.Combine would ignore our base path.
                // See https://docs.microsoft.com/en-us/dotnet/api/system.io.path.combine
                // (particularly the Remarks section).
                //
                // Under Windows, a relative URL path may be a full filesystem path
                // (e.g. "D:\foo\bar" or "\\192.168.0.1\Shared\MyDocuments\BankAccounts.docx").
                // Under Unix-like operating systems we have no such problems, as relativeUrlPath
                // can never start with a slash; however, loading one more class from Swan
                // just to check the OS type would probably outweigh calling IsPathRooted.
                if (Path.IsPathRooted(relativeUrlPath))
                {
                    localPath = null;
                    return false;
                }

                // Convert the relative URL path to a relative filesystem path
                // (practically a no-op under Unix-like operating systems)
                // and combine it with our base local path to obtain a full path.
                localPath = Path.Combine(BaseLocalPath, relativeUrlPath.Replace('/', Path.DirectorySeparatorChar));

                // Use GetFullPath as an additional safety check
                // for relative paths that contain a rooted path
                // (e.g. "valid/path/C:\Windows\System.ini")
                localPath = Path.GetFullPath(localPath);
            }
            catch
            {
                // Both IsPathRooted and GetFullPath throw exceptions
                // if a path contains invalid characters or is otherwise invalid;
                // bail out in this case too, as the path would not exist on disk anyway.
                localPath = null;
                return false;
            }
#pragma warning restore CA1031

            // As a final precaution, check that the resulting local path
            // is inside the folder intended to be served.
            if (!localPath.StartsWith(BaseLocalPath, StringComparison.Ordinal))
            {
                localPath = null;
                return false;
            }

            return true;
        }
    }
}