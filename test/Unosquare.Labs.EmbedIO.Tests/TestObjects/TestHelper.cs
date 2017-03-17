namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Swan.Formatters;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Modules;

    public static class TestHelper
    {
        private const string Placeholder = "This is a placeholder";

        public const string BigDataFile = "bigdata.bin";

        public const string SmallDataFile = "smalldata.bin";

        public const string LowercaseFile = "abcdef.txt";

        public const string UppercaseFile = "ABCDEF.txt";

        public static string RootPath()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(StaticFilesModuleTest).GetTypeInfo().Assembly.Location);
            return Path.Combine(assemblyPath, "html");
        }

        public static byte[] GetBigData()
        {
            return File.Exists(Path.Combine(RootPath(), BigDataFile))
                ? File.ReadAllBytes(Path.Combine(RootPath(), BigDataFile))
                : null;
        }

        public static byte[] GetSmallData()
        {
            return File.Exists(Path.Combine(RootPath(), SmallDataFile))
                ? File.ReadAllBytes(Path.Combine(RootPath(), SmallDataFile))
                : null;
        }

        public static string SetupStaticFolder()
        {
            var rootPath = RootPath();

            if (Directory.Exists(rootPath) == false)
                Directory.CreateDirectory(rootPath);

            if (File.Exists(Path.Combine(rootPath, StaticFilesModule.DefaultDocumentName)) == false)
                File.WriteAllText(Path.Combine(rootPath, "index.html"), Resources.Index);

            if (Directory.Exists(Path.Combine(rootPath, "sub")) == false)
                Directory.CreateDirectory(Path.Combine(rootPath, "sub"));

            if (File.Exists(Path.Combine(rootPath, "sub", StaticFilesModule.DefaultDocumentName)) == false)
                File.WriteAllText(Path.Combine(rootPath, "sub", "index.html"), Resources.SubIndex);

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

        public static string GetStaticFolderInstanceIndexFileContents(string instanceName)
        {
            var content = Resources.Index;
            if (string.IsNullOrWhiteSpace(instanceName)) return content;

            Assert.AreEqual(true, content.Contains(Placeholder), "Setup error");
            return content.Replace(Placeholder, "Instance name is " + instanceName);
        }

        public static string SetupStaticFolderInstance(string instanceName)
        {
            var folderName = instanceName.Replace('/', Path.DirectorySeparatorChar);
            var folder =
                Path.Combine(Path.GetDirectoryName(typeof(StaticFilesModuleTest).GetTypeInfo().Assembly.Location),
                    folderName);

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
            const int blockSize = 1024*8;
            const int blocksPerMb = (1024*1024)/blockSize;
            var data = new byte[blockSize];

            var rng = new Random();
            using (var stream = File.OpenWrite(fileName))
            {
                // There 
                for (var i = 0; i < sizeInMb*blocksPerMb; i++)
                {
                    rng.NextBytes(data);
                    stream.Write(data, 0, data.Length);
                }
            }
        }

        public static async Task ValidatePerson(string url, Person person = null)
        {
            person = person ?? PeopleRepository.Database.First();

            using (var http = new HttpClient())
            {
                var jsonBody = await http.GetStringAsync(url);

                Assert.IsNotNull(jsonBody, "Json Body is not null");
                Assert.IsNotEmpty(jsonBody, "Json Body is not empty");

                var item = Json.Deserialize<Person>(jsonBody);

                Assert.IsNotNull(item, "Json Object is not null");
                Assert.AreEqual(item.Name, person.Name, "Remote objects equality");
                Assert.AreEqual(item.Name, PeopleRepository.Database.First().Name, "Remote and local objects equality");
            }
        }
    }
}