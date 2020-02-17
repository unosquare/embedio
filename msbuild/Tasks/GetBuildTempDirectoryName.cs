#line 2 "GetTempBuildDirectoryName.cs"

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BuildTasks
{
    public sealed class ParseVersionFile : Task
    {
        #region Public API

        [Required]
        public string ProjectFullPath { get; set; }

        [Output]
        public string BuildTempDirectoryName { get; private set; }

        #endregion

        #region Task overrides

        public override bool Execute()
        {
            try
            {
                var sb = new StringBuilder()
                    .Append(Path.GetFileNameWithoutExtension(ProjectFullPath))
                    .Append('_');

                using (var algorithm = new SHA256CryptoServiceProvider())
                {
                    var data = algorithm.ComputeHash(Encoding.UTF8.GetBytes(ProjectFullPath));

                    foreach (var b in data)
                        sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                }

                BuildTempDirectoryName = sb.ToString();
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, true, true, "GetBuildTempDirectoryName.cs");
            }

            return !Log.HasLoggedErrors;
        }

        #endregion
    }
}