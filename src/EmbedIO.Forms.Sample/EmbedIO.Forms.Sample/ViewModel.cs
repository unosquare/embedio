using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace EmbedIO.Forms.Sample
{
    public class ViewModel : INotifyPropertyChanged
    {
        private const string DefaultUrl = "http://localhost:8080/";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _result;
        public string Result
        {
            get { return _result; }
            set
            {
                _result = value;
                OnPropertyChanged("Result");
            }
        }

        public ICommand AcceptEncoding_None_Command { get; set; }
        public ICommand AcceptEncoding_Gzip_Command { get; set; }

        public ViewModel()
        {
            AcceptEncoding_None_Command = new Command(async () => await AcceptEncoding_None());
            AcceptEncoding_Gzip_Command = new Command(async () => await AcceptEncoding_Gzip());

            Result = "Result will appear here";
        }

        private async Task AcceptEncoding_None()
        {
            try
            {
                Result = $"Trying AcceptEncoding = None{System.Environment.NewLine}";

                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync($"{DefaultUrl}api/testresponse").ConfigureAwait(false))
                    {
                        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Result += "Result = " + (string.IsNullOrEmpty(responseString) ? "<Empty>" : responseString);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async Task AcceptEncoding_Gzip()
        {
            try
            {
                Result = $"Trying AcceptEncoding = Gzip{System.Environment.NewLine}";

                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                };

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

                    using (var response = await client.GetAsync($"{DefaultUrl}api/testresponse").ConfigureAwait(false))
                    {
                        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        Result += "Result = " + (string.IsNullOrEmpty(responseString) ? "<Empty>" : responseString);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
