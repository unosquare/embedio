using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Modules;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace EmbedIO.Forms.Sample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            Task.Factory.StartNew(async () =>
            {
                using (var server = new WebServer("http://" + GetLocalIpAddress() + ":8080"))
                {
                    server.RegisterModule(new LocalSessionModule());
                    server.Module<StaticFilesModule>().UseRamCache = true;
                    server.Module<StaticFilesModule>().DefaultExtension = ".html";
                    server.Module<StaticFilesModule>().DefaultDocument = "index.html";
                    await server.RunAsync();
                }
            });
        }
        
        private static string GetLocalIpAddress()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Address.ToString();
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
