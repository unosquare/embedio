namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using EmbedIO;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Swan;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Reflection;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// Represents a simple module to server resource files from the .NET assembly.
    /// </summary>
    public class ResourceFilesModule
        : FileModuleBase
    {
        private readonly Assembly _sourceAssembly;
        private readonly string _resourcePathRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFilesModule" /> class.
        /// </summary>
        /// <param name="sourceAssembly">The source assembly.</param>
        /// <param name="resourcePath">The resource path.</param>
        /// <param name="headers">The headers.</param>
        /// <exception cref="ArgumentNullException">sourceAssembly.</exception>
        /// <exception cref="ArgumentException">Path ' + fileSystemPath + ' does not exist.</exception>
        public ResourceFilesModule(
            Assembly sourceAssembly,
            string resourcePath,
            Dictionary<string, string> headers = null)
        {
            if (sourceAssembly == null)
                throw new ArgumentNullException(nameof(sourceAssembly));

            if (sourceAssembly.GetName() == null)
                throw new ArgumentException($"Assembly '{sourceAssembly}' not valid.");

            UseGzip = true;
            _sourceAssembly = sourceAssembly;
            _resourcePathRoot = resourcePath;

            headers?.ForEach(DefaultHeaders.Add);

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Head, (context, ct) => HandleGet(context, ct, false));
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, (context, ct) => HandleGet(context, ct));
        }

        /// <inheritdoc />
        public override string Name => nameof(ResourceFilesModule).Humanize();

        private static string PathResourcerize(string s) => s == "/" ? "index.html" : s.Substring(1, s.Length - 1).Replace('/', '.');

        private async Task<bool> HandleGet(HttpListenerContext context, CancellationToken ct, bool sendBuffer = true)
        {
            Stream buffer = null;

            try
            {
                var localPath = PathResourcerize(context.RequestPathCaseSensitive());
                var partialHeader = context.RequestHeader(Headers.Range);

                $"Resource System: {localPath}".Debug();

                buffer = _sourceAssembly.GetManifestResourceStream($"{_resourcePathRoot}.{localPath}");

                // If buffer is null something is really wrong
                if (buffer == null)
                {
                    return false;
                }

                // check to see if the file was modified or e-tag is the same
                var utcFileDateString = DateTime.Now.ToUniversalTime()
                    .ToString(Strings.BrowserTimeFormat, Strings.StandardCultureInfo);

                SetHeaders(context.Response, localPath, utcFileDateString);

                // HEAD (file size only)
                if (sendBuffer == false)
                {
                    context.Response.ContentLength64 = buffer.Length;
                    return true;
                }

                await WriteFileAsync(partialHeader?.StartsWith("bytes=") == true, partialHeader, buffer.Length, context, buffer, ct);
            }
            catch (HttpListenerException)
            {
                // Connection error, nothing else to do
            }
            finally
            {
                buffer?.Dispose();
            }

            return true;
        }

        private void SetHeaders(HttpListenerResponse response, string localPath, string utcFileDateString)
        {
            var fileExtension = localPath.Contains(".") ? $".{localPath.Split('.').Last()}" : ".html";

            if (MimeTypes.Value.ContainsKey(fileExtension))
                response.ContentType = MimeTypes.Value[fileExtension];

            SetDefaultCacheHeaders(response);

            response.AddHeader(Headers.LastModified, utcFileDateString);
            response.AddHeader(Headers.AcceptRanges, "bytes");
        }
    }
}