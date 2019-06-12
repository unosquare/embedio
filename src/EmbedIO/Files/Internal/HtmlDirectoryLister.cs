using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Swan;

namespace EmbedIO.Files.Internal
{
    internal class HtmlDirectoryLister : IDirectoryLister
    {
        private static readonly Lazy<IDirectoryLister> LazyInstance = new Lazy<IDirectoryLister>(() => new HtmlDirectoryLister());

        private HtmlDirectoryLister()
        {
        }

        public static IDirectoryLister Instance => LazyInstance.Value;

        public string ContentType { get; } = MimeTypes.HtmlType + "; encoding=" + Encoding.UTF8.WebName;

        public Task ListDirectoryAsync(
            MappedDirectoryInfo info,
            string absoluteUrlPath,
            IEnumerable<MappedResourceInfo> entries, 
            Stream stream,
            CancellationToken cancellationToken)
        {
            const int MaxEntryLength = 50;
            const int SizeIndent = -20; // Negative for right alignment

            var encodedPath = WebUtility.HtmlEncode(absoluteUrlPath);
            using (var text = new StreamWriter(stream, Encoding.UTF8))
            {
                text.Write("<html><head><title>Index of ");
                text.Write(encodedPath);
                text.Write("</title></head><body><h1>Index of ");
                text.Write(encodedPath);
                text.Write("</h1><hr/><pre>");

                if (encodedPath.Length > 1)
                    text.Write("<a href='../'>../</a>\n");

                entries = entries.ToArray();

                foreach (var directory in entries.OfType<MappedDirectoryInfo>().OrderBy(e => e.Name))
                {
                    text.Write($"<a href=\"{Uri.EscapeDataString(directory.Name)}{Path.DirectorySeparatorChar}\">{WebUtility.HtmlEncode(directory.Name)}</a>");
                    text.Write(new string(' ', Math.Max(1, MaxEntryLength - directory.Name.Length + 1)));
                    text.Write(directory.LastWriteTimeUtc.ToRfc1123String());
                    text.Write('\n');
                }

                foreach (var file in entries.OfType<MappedFileInfo>().OrderBy(e => e.Name))
                {
                    text.Write($"<a href=\"{Uri.EscapeDataString(file.Name)}{Path.DirectorySeparatorChar}\">{WebUtility.HtmlEncode(file.Name)}</a>");
                    text.Write(new string(' ', Math.Max(1, MaxEntryLength - file.Name.Length + 1)));
                    text.Write(file.LastWriteTimeUtc.ToRfc1123String());
                    text.Write($" {file.Size.ToString("#,###", CultureInfo.InvariantCulture),SizeIndent}\n");
                }

                text.Write("</pre><hr/></body></html>");
            }

            return Task.CompletedTask;
        }
    }
}