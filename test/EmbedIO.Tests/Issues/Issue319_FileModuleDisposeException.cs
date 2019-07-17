using EmbedIO.Files;
using EmbedIO.Testing;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Issues
{
    public class Issue319_FileModuleDisposeException
    {
        [Test]
        public void FileModule_Dispose_WhenNotStarted_DoesNotThrow()
        {
            var module = new FileModule(UrlPath.Root, new MockFileProvider());
            Assert.DoesNotThrow(() => module.Dispose());
        }
    }
}