namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System;
    using System.IO;
    using Unosquare.Labs.EmbedIO.Tests.Properties;

    public static class TestHelper
    {
        public static readonly bool IsMono = Type.GetType("Mono.Runtime") != null;

        public const string BigDataFile = "bigdata.bin";

        public const string SmallDataFile = "smalldata.bin";

        private static string RootPath()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(StaticFilesModuleTest).Assembly.Location);
            return Path.Combine(assemblyPath, "html");
        }

        public static byte[] GetBigData()
        {
            return File.Exists(Path.Combine(RootPath(), BigDataFile)) ? File.ReadAllBytes(Path.Combine(RootPath(), BigDataFile)) : null;
        }

        public static byte[] GetSmallData()
        {
            return File.Exists(Path.Combine(RootPath(), SmallDataFile)) ? File.ReadAllBytes(Path.Combine(RootPath(), SmallDataFile)) : null;
        }

        public static string SetupStaticFolder()
        {
            var rootPath = RootPath();

            if (Directory.Exists(rootPath) == false)
                Directory.CreateDirectory(rootPath);

            if (File.Exists(Path.Combine(rootPath, "index.html")) == false)
                File.WriteAllText(Path.Combine(rootPath, "index.html"), Resources.index);

            if (File.Exists(Path.Combine(rootPath, BigDataFile)) == false)
                CreateTempBinaryFile(Path.Combine(rootPath, BigDataFile), 100);

            if (File.Exists(Path.Combine(rootPath, SmallDataFile)) == false)
                CreateTempBinaryFile(Path.Combine(rootPath, SmallDataFile), 2);

            return rootPath;
        }

        public static void CreateTempBinaryFile(string fileName, int sizeInMb)
        {
            // Note: block size must be a factor of 1MB to avoid rounding errors :)
            const int blockSize = 1024*8;
            const int blocksPerMb = (1024*1024)/blockSize;
            byte[] data = new byte[blockSize];

            var rng = new Random();
            using (FileStream stream = File.OpenWrite(fileName))
            {
                // There 
                for (int i = 0; i < sizeInMb*blocksPerMb; i++)
                {
                    rng.NextBytes(data);
                    stream.Write(data, 0, data.Length);
                }
            }
        }
    }
}