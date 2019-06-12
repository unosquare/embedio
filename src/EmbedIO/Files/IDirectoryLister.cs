using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Files
{
    public interface IDirectoryLister
    {
        string ContentType { get; }

        Task ListDirectoryAsync(
            MappedDirectoryInfo info,
            string absoluteUrlPath,
            IEnumerable<MappedResourceInfo> entries,
            Stream stream,
            CancellationToken cancellationToken);
    }
}