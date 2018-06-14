namespace Unosquare.Labs.EmbedIO.Command
{
    using Swan.Attributes;

    /// <summary>
    /// CLI options container
    /// </summary>
    internal class Options
    {
        [ArgumentOption('p', "path", HelpText = "WWW-root path.")]
        public string RootPath { get; set; }

        [ArgumentOption('o', "port", HelpText = "HTTP port.", DefaultValue = 9696)]
        public int Port { get; set; }
        
        [ArgumentOption('a', "api", HelpText = "Specify assembly to load.")]
        public string ApiAssemblies { get; set; }

        [ArgumentOption("no-watch", DefaultValue = false, HelpText = "Disable watch filesystem" )]
        public bool NoWatch { get; set; }
    }
}