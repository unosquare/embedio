namespace Unosquare.Labs.EmbedIO.Command
{
    using Swan.Attributes;

    /// <summary>
    /// CLI options container
    /// </summary>
    internal class Options
    {
        [ArgumentOption('p', "path", Required = true, HelpText = "WWW-root path.")]
        public string RootPath { get; set; }

        [ArgumentOption('o', "port", HelpText = "HTTP port.", DefaultValue = 9696)]
        public int Port { get; set; }
        
        [ArgumentOption('a', "api", HelpText = "Specify assembly to load.")]
        public string ApiAssemblies { get; set; }

        [ArgumentOption('v', "noverb", HelpText = "Output Web server info.", DefaultValue = false)]
        public bool NoVerbose { get; set; } // TODO: implement with SWAN
    }
}