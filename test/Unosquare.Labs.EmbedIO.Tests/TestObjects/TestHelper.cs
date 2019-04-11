namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using Modules;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public static class TestHelper
    {
        public const string BigDataFile = "bigdata.bin";

        public const string SmallDataFile = "smalldata.bin";

        public const string LowercaseFile = "abcdef.txt";

        public const string UppercaseFile = "ABCDEF.txt";

        public static string[] RandomHtmls = {"abc.html", "wkp.html", "zxy.html"};

        private const string Placeholder = "This is a placeholder";

        public static string RootPath(string folderName)
        {
            var assemblyPath = Path.GetDirectoryName(typeof(StaticFilesModuleTest).GetTypeInfo().Assembly.Location);
            return Path.Combine(assemblyPath ?? throw new InvalidOperationException(), folderName);
        }

        public static string RootPath() => RootPath("html");

        public static byte[] GetBigData() => File.Exists(Path.Combine(RootPath(), BigDataFile))
            ? File.ReadAllBytes(Path.Combine(RootPath(), BigDataFile))
            : null;

        private static string SetupStaticFolderCore(string rootPath, bool onlyIndex = true)
        {
            if (Directory.Exists(rootPath) == false)
                Directory.CreateDirectory(rootPath);

            if (Directory.Exists(Path.Combine(rootPath, "sub")) == false)
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

            if (File.Exists(Path.Combine(rootPath, BigDataFile)) == false)
                CreateTempBinaryFile(Path.Combine(rootPath, BigDataFile), 10);

            if (File.Exists(Path.Combine(rootPath, SmallDataFile)) == false)
                CreateTempBinaryFile(Path.Combine(rootPath, SmallDataFile), 1);

            if (File.Exists(Path.Combine(rootPath, LowercaseFile)) == false)
                File.WriteAllText(Path.Combine(rootPath, LowercaseFile), nameof(LowercaseFile));

            if (File.Exists(Path.Combine(rootPath, UppercaseFile)) == false)
                File.WriteAllText(Path.Combine(rootPath, UppercaseFile), nameof(UppercaseFile));

            return rootPath;
        }

        public static string SetupStaticFolder(bool onlyIndex = true) => SetupStaticFolderCore(RootPath(), onlyIndex);

        public static string SetupStaticFolder(string folderName, bool onlyIndex = true) => SetupStaticFolderCore(RootPath(folderName), onlyIndex);

        public static string GetStaticFolderInstanceIndexFileContents(string instanceName) =>
            string.IsNullOrWhiteSpace(instanceName)
                ? Resources.Index
                : Resources.Index.Replace(Placeholder, "Instance name is " + instanceName);

        public static string SetupStaticFolderInstance(string instanceName)
        {
            var folderName = instanceName.Replace('/', Path.DirectorySeparatorChar);
            var location = Path.GetDirectoryName(typeof(StaticFilesModuleTest).GetTypeInfo().Assembly.Location) ??
                           throw new InvalidOperationException();
            var folder = Path.Combine(location, folderName);

            if (Directory.Exists(folder) == false)
                Directory.CreateDirectory(folder);

            var fileName = Path.Combine(folder, StaticFilesModule.DefaultDocumentName);

            File.WriteAllText(fileName, GetStaticFolderInstanceIndexFileContents(instanceName));
            return folder;
        }

        /// <summary>
        /// Creates the temporary binary file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="sizeInMb">The size in mb.</param>
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