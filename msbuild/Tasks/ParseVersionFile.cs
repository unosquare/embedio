#line 2 "ParseVersionFile.cs"

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BuildTasks
{
    sealed class SemVersion
    {
        #region Private data

        static readonly Regex SemVerRegex =new Regex(
            @"^(?<major>\d+)" +
            @"(?>\.(?<minor>\d+))?" +
            @"(?>\.(?<patch>\d+))?" +
            @"(?>\-(?<pre>[0-9A-Za-z\-\.]+))?" +
            @"(?>\+(?<build>[0-9A-Za-z\-\.]+))?$",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        #endregion

        #region Instance management

        SemVersion(int major, int minor = 0, int patch = 0, string prerelease = "", string build = "")
        {
            Major = major;
            Minor = minor;
            Patch = patch;

            Prerelease = prerelease ?? string.Empty;
            Build = build ?? string.Empty;
        }

        #endregion

        #region Public API

        public static SemVersion Parse(string version)
        {
            var exception = TryParseInternal(version, out var result);
            if (exception != null)
                throw exception;

            return result;
        }

        public static bool TryParse(string version, out SemVersion result)
            => TryParseInternal(version, out result) == null;

        public int Major { get; }

        public int Minor { get; }

        public int Patch { get; }

        public string Prerelease { get; }

        public string Build { get; }

        #endregion

        #region Overrides of Object

        public override string ToString()
        {
            var sb = new StringBuilder()
                .Append(Major)
                .Append('.')
                .Append(Minor)
                .Append('.')
                .Append(Patch);

            if (Prerelease.Length > 0)
                sb.Append('-').Append(Prerelease);

            if (Build.Length > 0)
                sb.Append('+').Append(Build);

            return sb.ToString();
        }

        #endregion

        #region Private API

        static Exception TryParseInternal(string version, out SemVersion result)
        {
            result = null;

            var match = SemVerRegex.Match(version);
            if (!match.Success)
                return new FormatException("Invalid version.");

            if (!int.TryParse(match.Groups["major"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var major))
                return new FormatException("Invalid version: major version is not a non-negative integer number.");

            var minorMatch = match.Groups["minor"];
            if (!minorMatch.Success)
                return new FormatException("Invalid version: no minor version given.");

            if (!int.TryParse(minorMatch.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
                return new FormatException("Invalid version: minor version is not a non-negative integer number.");

            var patchMatch = match.Groups["patch"];
            if (!patchMatch.Success)
                return new FormatException("Invalid version: no patch number given.");

            if (!int.TryParse(patchMatch.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var patch))
                return new FormatException("Invalid version: patch number is not a non-negative integer number.");

            var prerelease = match.Groups["pre"].Value;
            var build = match.Groups["build"].Value;

            result = new SemVersion(major, minor, patch, prerelease, build);
            return null;
        }

        #endregion
    }

    public sealed class ParseVersionFile : Task
    {
        #region Public API

        [Required]
        public string VersionFileFullPath { get; set; }

        [Output]
        public string Version { get; private set; }

        [Output]
        public string VersionPrefix { get; private set; }

        [Output]
        public string VersionSuffix { get; private set; }

        [Output]
        public string AssemblyVersion { get; private set; }

        [Output]
        public string AssemblyFileVersion { get; private set; }

        [Output]
        public string AssemblyInformationalVersion { get; private set; }

        #endregion

        #region Task overrides

        public override bool Execute()
        {
            try
            {
                var versionText = File.ReadAllText(VersionFileFullPath.Trim()).Trim();
                var semVersion = SemVersion.Parse(versionText);

                Version = semVersion.ToString();
                VersionPrefix = $"{semVersion.Major}.{semVersion.Minor}.{semVersion.Patch}";
                VersionSuffix = semVersion.Prerelease;
                AssemblyVersion = $"{semVersion.Major}.0.0.0";
                AssemblyFileVersion = $"{semVersion.Major}.{semVersion.Minor}.{semVersion.Patch}.0";
                AssemblyInformationalVersion = semVersion.ToString();
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, true, true, "ParseVersionFile.cs");
            }

            return !Log.HasLoggedErrors;
        }

        #endregion
    }
}