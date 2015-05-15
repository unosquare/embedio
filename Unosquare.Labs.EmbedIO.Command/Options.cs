using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace Unosquare.Labs.EmbedIO.Command
{
    /// <summary>
    /// CLI options container
    /// </summary>
    internal class Options
    {
        [Option('p', "path", Required = true, HelpText = "WWW-root path.")]
        public string RootPath { get; set; }

        [Option('o', "port", HelpText = "HTTP port.", DefaultValue=9696)]
        public int Port { get; set; }

        [OptionList('a', "api", Separator = ',', HelpText = "Specify assemblies to load, separated by a comma.")]
        public IList<string> ApiAssemblies { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}