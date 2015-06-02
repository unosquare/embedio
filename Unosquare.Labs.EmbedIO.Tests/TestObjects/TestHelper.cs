namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System.IO;
    using Unosquare.Labs.EmbedIO.Tests.Properties;

    public static class TestHelper
    {
        public static string SetupStaticFolder()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(StaticFilesModuleTest).Assembly.Location);
            var rootPath = Path.Combine(assemblyPath, "html");

            if (Directory.Exists(rootPath) == false)
                Directory.CreateDirectory(rootPath);

            if (File.Exists(Path.Combine(rootPath, "index.html")) == false)
                File.WriteAllText(Path.Combine(rootPath, "index.html"), Resources.index);

            return rootPath;
        }
    }
}
