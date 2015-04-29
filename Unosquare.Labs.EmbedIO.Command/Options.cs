using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace Unosquare.Labs.EmbedIO.Command
{
    internal class Options
    {
        // TODO: Get a JSON file?
        //[ValueList(typeof (List<string>), MaximumElements = 1)]
        //public IList<string> Items { get; set; }

        [Option('p', "path", Required = true, HelpText = "WWW-root path.")]
        public string RootPath { get; set; }

        [OptionList('a', "api", Separator = ',', HelpText = "Specify assemblies to load, separated by a comma.")]
        public IList<string> ApiAssemblies { get; set; }

        // TODO: Add url

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}