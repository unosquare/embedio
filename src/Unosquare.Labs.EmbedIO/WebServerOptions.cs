﻿namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using Swan;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Options for WebServer creation.
    /// </summary>
    public sealed class WebServerOptions
    {
        private X509Certificate2 _certificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerOptions" /> class.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        public WebServerOptions(string urlPrefix)
            : this(new[] { urlPrefix })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerOptions"/> class.
        /// </summary>
        /// <param name="urlPrefixes">The urls.</param>
        public WebServerOptions(string[] urlPrefixes)
        {
            UrlPrefixes = urlPrefixes;
        }

        /// <summary>
        /// Gets the URL prefixes.
        /// </summary>
        /// <value>
        /// The URL prefixes.
        /// </value>
        public string[] UrlPrefixes { get; }

        /// <summary>
        /// Gets or sets the routing strategy.
        /// </summary>
        /// <value>
        /// The routing strategy.
        /// </value>
        public RoutingStrategy RoutingStrategy { get; set; } = RoutingStrategy.Regex;

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>
        /// The mode.
        /// </value>
        public HttpListenerMode Mode { get; set; } = HttpListenerMode.EmbedIO;

#if !NETSTANDARD1_3 && !UWP
        /// <summary>
        /// Gets or sets the certificate.
        /// </summary>
        /// <value>
        /// The certificate.
        /// </value>
        public X509Certificate2 Certificate
        {
            get
            {
                if (AutoRegisterCertificate)
                {
                    return TryRegisterCertificate() ? _certificate : null;
                }

                return _certificate == null && AutoLoadCertificate ? LoadCertificate() : _certificate;
            }

            set => _certificate = value;
        }

        /// <summary>
        /// Gets or sets the certificate thumb.
        /// </summary>
        /// <value>
        /// The certificate thumb.
        /// </value>
        public string CertificateThumb { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [automatic load certificate].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [automatic load certificate]; otherwise, <c>false</c>.
        /// </value>
        public bool AutoLoadCertificate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [automatic register certificate].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [automatic register certificate]; otherwise, <c>false</c>.
        /// </value>
        public bool AutoRegisterCertificate { get; set; }

        private X509Certificate2 LoadCertificate()
        {
            if (!string.IsNullOrWhiteSpace(CertificateThumb)) return GetCertificate();

            var netsh = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments =
                        $"http show sslcert ipport=0.0.0.0:{GetSslPort()}",
                    RedirectStandardOutput = true,
                },
            };

            var thumbPrint = string.Empty;

            netsh.OutputDataReceived += (sender, eventArgs) =>
            {
                if (eventArgs.Data == null)
                    return;

                var line = eventArgs.Data?.Trim();

                if (line.StartsWith("Certificate Hash") && line.IndexOf(":", StringComparison.Ordinal) > -1)
                    thumbPrint = line.Split(':')[1].Trim();
            };

            if (netsh.Start())
            {
                netsh.BeginOutputReadLine();
                netsh.WaitForExit();

                if (netsh.ExitCode == 0 && !string.IsNullOrWhiteSpace(thumbPrint))
                {
                    return GetCertificate(thumbPrint);
                }
            }

            return null;
        }

        private X509Certificate2 GetCertificate(string thumb = null)
        {
            // strip any non-hexadecimal values and make uppercase
            var thumbprint = Regex.Replace(thumb ?? CertificateThumb, @"[^\da-fA-F]", string.Empty).ToUpper();
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            try
            {
                store.Open(OpenFlags.ReadOnly);

                var signingCert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

                return signingCert.Count == 0 ? null : signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }

        private bool AddCertificateToStore()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            try
            {
                store.Open(OpenFlags.ReadWrite);

                store.Add(_certificate);
            }
            catch
            {
                return false;
            }
            finally
            {
                store.Close();
            }

            return true;
        }

        private bool TryRegisterCertificate()
        {
            if (Runtime.OS != Swan.OperatingSystem.Windows)
                throw new InvalidOperationException("AutoRegister functionality is only available in Windows");

            if (_certificate == null)
                throw new InvalidOperationException("A certificate is required to AutoRegister");

            if (GetCertificate(_certificate.Thumbprint) == null && !AddCertificateToStore())
                    throw new InvalidOperationException("The provided certificate cannot be added to the default store, add it manually");

            var netsh = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments =
                        $"http add sslcert ipport=0.0.0.0:{GetSslPort()} certhash={_certificate.Thumbprint} appid={{adaa04bb-8b63-4073-a12f-d6f8c0b4383f}}",
                },
            };

            if (netsh.Start())
            {
                netsh.WaitForExit();

                return netsh.ExitCode == 0;
            }

            return false;
        }

        private int GetSslPort()
        {
            var port = 443;

            foreach (var url in UrlPrefixes.Where(x => x.StartsWith("https", StringComparison.InvariantCultureIgnoreCase)))
            {
                var match = Regex.Match(url, @":(\d+)");

                if (match.Success && int.TryParse(match.Groups[1].Value, out port))
                    break;
            }

            return port;
        }
#endif
    }
}