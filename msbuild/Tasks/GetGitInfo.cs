#line 2 "GetGitInfo.cs"

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BuildTasks
{
    public sealed class GetGitInfo : Task
    {
        #region Public API

        [Required]
        public string RepoDirectory { get; set; }

        [Output]
        public bool IsGitRepo { get; private set; }

        [Output]
        public string RepoUrl { get; private set; }

        [Output]
        public string Branch { get; private set; }

        [Output]
        public string CommitSHA { get; private set; }

        [Output]
        public string CommitShort { get; private set; }

        #endregion

        #region Task overrides

        public override bool Execute()
        {
            try
            {
                IsGitRepo = false;
                if (!Directory.Exists(Path.Combine(RepoDirectory, ".git")))
                    return true;

                IsGitRepo = true;
                var previousCurrentDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(RepoDirectory);
                try
                {
                    RunGit("remote", out var remoteNames);
                    var remotes = remoteNames.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string remote = null;
                    if (remotes.Contains("upstream"))
                        remote = "upstream";
                    else if (remotes.Contains("origin"))
                        remote = "origin";
                    else
                        throw new Exception("Cannot determine the Git remote to use.");

                    RunGit("remote get-url " + remote, out var repoUrl);
                    RepoUrl = repoUrl;
                    
                    RunGit("name-rev --name-only HEAD", out var branch);
                    Branch = branch;

                    RunGit("rev-parse HEAD", out var commitSHA);
                    CommitSHA = commitSHA;

                    RunGit("rev-parse --short HEAD", out var commitShort);
                    CommitShort = commitShort;
                }
                finally
                {
                    Directory.SetCurrentDirectory(previousCurrentDirectory);
                }
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, true, true, "GetGitInfo.cs");
            }

            return !Log.HasLoggedErrors;
        }

        #endregion

        #region Private API

        static void RunGit(string arguments, out string output)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "git";
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }

            output = output.Trim();
        }

        #endregion
    }
}