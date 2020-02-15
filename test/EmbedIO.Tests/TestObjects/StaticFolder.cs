using System;
using System.Collections.Generic;
using System.IO;
using EmbedIO.Files;

namespace EmbedIO.Tests.TestObjects
{
    public abstract class StaticFolder : IDisposable
    {
        protected StaticFolder(string folderName)
        {
            RootPath = RootPathOf(folderName);
            Directory.CreateDirectory(RootPath);
            Directory.CreateDirectory(PathOf("sub"));

            File.WriteAllText(PathOf(FileModule.DefaultDocumentName), Resources.Index);
            File.WriteAllText(PathOf("sub", FileModule.DefaultDocumentName), Resources.SubIndex);
        }

        ~StaticFolder()
        {
            Dispose(false);
        }

        public string RootPath { get; }

        public static string RootPathOf(string folderName)
        {
            var assemblyPath = Path.GetDirectoryName(typeof(FileModuleTest).Assembly.Location);
            return Path.Combine(assemblyPath ?? Path.GetTempPath(), folderName);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Directory.Delete(RootPath, true);
        }

        protected string PathOf(string path) => Path.Combine(RootPath, path);

        protected string PathOf(string path1, string path2) => Path.Combine(RootPath, path1, path2);
        
        public sealed class WithDataFiles : StaticFolder
        {
            public const string BigDataFile = "bigdata.bin";
            public const int BigDataSize = BigDataSizeMb * 1024 * 1024;

            public const string SmallDataFile = "smalldata.bin";
            public const int SmallDataSize = SmallDataSizeMb * 1024 * 1024;

            public const string LowercaseFile = "abcdef.txt";

            public const string UppercaseFile = "ABCDEF.txt";

            private const int BigDataSizeMb = 10;
            private const int SmallDataSizeMb = 1;

            public WithDataFiles(string folderName)
                : base(folderName)
            {
                var bigData = CreateRandomData(BigDataSize);
                File.WriteAllBytes(PathOf(BigDataFile), bigData);
                BigData = bigData;

                var smallData = CreateRandomData(SmallDataSize);
                File.WriteAllBytes(PathOf(SmallDataFile), smallData);
                SmallData = smallData;

                File.WriteAllText(PathOf(LowercaseFile), nameof(LowercaseFile));
                File.WriteAllText(PathOf(UppercaseFile), nameof(UppercaseFile));
            }

            public IReadOnlyList<byte> BigData { get; }

            public IReadOnlyList<byte> SmallData { get; }

            private static byte[] CreateRandomData(int size)
            {
                var rng = new Random();
                var data = new byte[size];
                rng.NextBytes(data);
                return data;
            }
        }

        public sealed class WithHtmlFiles : StaticFolder
        {
            public static readonly IReadOnlyList<string> RandomHtmls = new[] { "abc.html", "wkp.html", "zxy.html" };

            public WithHtmlFiles(string folderName)
                : base(folderName)
            {
                foreach (var file in RandomHtmls)
                {
                    File.WriteAllText(PathOf(file), Resources.Index);
                    File.WriteAllText(PathOf("sub", file), Resources.SubIndex);
                }
            }
        }
    }
}