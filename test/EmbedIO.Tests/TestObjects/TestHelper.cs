using System;
using System.IO;
using System.Linq;
using EmbedIO.Files;

namespace EmbedIO.Tests.TestObjects
{
    public static class TestHelper
    {
        public const string BigDataFile = "bigdata.bin";

        public const string SmallDataFile = "smalldata.bin";

        public const string LowercaseFile = "abcdef.txt";

        public const string UppercaseFile = "ABCDEF.txt";

        public static string[] RandomHtmls = {"abc.html", "wkp.html", "zxy.html"};

        public static string RootPath(string folderName)
        {
            var assemblyPath = Path.GetDirectoryName(typeof(StaticFilesModuleTest).Assembly.Location);
            return Path.Combine(assemblyPath ?? throw new InvalidOperationException(), folderName);
        }

        public static string RootPath() => RootPath("html");

        public static byte[] GetBigData() => File.Exists(Path.Combine(RootPath(), BigDataFile))
            ? File.ReadAllBytes(Path.Combine(RootPath(), BigDataFile))
            : null;

        private static string SetupStaticFolderCore(string rootPath, bool onlyIndex = true)
        {
            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            if (!Directory.Exists(Path.Combine(rootPath, "sub")))
                Directory.CreateDirectory(Path.Combine(rootPath, "sub"));

            var files = onlyIndex ? new[] {StaticFilesModule.DefaultDocumentName} : RandomHtmls;

            foreach (var file in files.Where(file => !File.Exists(Path.Combine(rootPath, file))))
            {
                File.WriteAllText(Path.Combine(rootPath, file), Resources.Index);
            }

            foreach (var file in files.Where(file => !File.Exists(Path.Combine(rootPath, "sub", file))))
            {
                File.WriteAllText(Path.Combine(rootPath, "sub", file), Resources.SubIndex);
            }

            // write only random htmls when onlyIndex is false
            if (!onlyIndex) return rootPath;

            if (!File.Exists(Path.Combine(rootPath, BigDataFile)))
                CreateTempBinaryFile(Path.Combine(rootPath, BigDataFile), 10);

            if (!File.Exists(Path.Combine(rootPath, SmallDataFile)))
                CreateTempBinaryFile(Path.Combine(rootPath, SmallDataFile), 1);

            if (!File.Exists(Path.Combine(rootPath, LowercaseFile)))
                File.WriteAllText(Path.Combine(rootPath, LowercaseFile), nameof(LowercaseFile));

            if (!File.Exists(Path.Combine(rootPath, UppercaseFile)))
                File.WriteAllText(Path.Combine(rootPath, UppercaseFile), nameof(UppercaseFile));

            return rootPath;
        }

        public static string SetupStaticFolder(string testName, bool onlyIndex = true) => SetupStaticFolderCore(RootPath(testName), onlyIndex);

        public static void CreateTempBinaryFile(string fileName, int sizeInMb)
        {
            // Note: block size must be a factor of 1MB to avoid rounding errors :)
            const int blockSize = 1024 * 8;
            const int blocksPerMb = (1024 * 1024) / blockSize;
            var data = new byte[blockSize];

            var rng = new Random();
            using (var stream = File.OpenWrite(fileName))
            {
                // There 
                for (var i = 0; i < sizeInMb * blocksPerMb; i++)
                {
                    rng.NextBytes(data);
                    stream.Write(data, 0, data.Length);
                }
            }
        }
    }
}