using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for <see cref="WebServerOptions"/>.
    /// </summary>
    public static class WebServerOptionsExtensions
    {
        /// <summary>
        /// Adds a URL prefix.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <returns><paramref name="this"/> with <paramref name="urlPrefix"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="urlPrefix"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="urlPrefix"/> is the empty string.</para>
        /// <para>- or -</para>
        /// <para><paramref name="urlPrefix"/> is already registered.</para>
        /// </exception>
        public static WebServerOptions WithUrlPrefix(this WebServerOptions @this, string urlPrefix)
        {
            @this.AddUrlPrefix(urlPrefix);
            return @this;
        }

        /// <summary>
        /// Adds zero or more URL prefixes.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="urlPrefixes">An enumeration of URL prefixes to add.</param>
        /// <returns><paramref name="this"/> with every non-<see langword="null"/> element
        /// of <paramref name="urlPrefixes"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="urlPrefixes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is the empty string.</para>
        /// <para>- or -</para>
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is already registered.</para>
        /// </exception>
        public static WebServerOptions WithUrlPrefixes(this WebServerOptions @this, IEnumerable<string> urlPrefixes)
        {
            foreach (var urlPrefix in Validate.NotNull(nameof(urlPrefixes), urlPrefixes))
                @this.AddUrlPrefix(urlPrefix);

            return @this;
        }

        /// <summary>
        /// Adds zero or more URL prefixes.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="urlPrefixes">An array of URL prefixes to add.</param>
        /// <returns><paramref name="this"/> with every non-<see langword="null"/> element
        /// of <paramref name="urlPrefixes"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="urlPrefixes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is the empty string.</para>
        /// <para>- or -</para>
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is already registered.</para>
        /// </exception>
        public static WebServerOptions WithUrlPrefixes(this WebServerOptions @this, params string[] urlPrefixes)
            => WithUrlPrefixes(@this, urlPrefixes as IEnumerable<string>);

        /// <summary>
        /// Sets the type of HTTP listener.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="value">The type of HTTP listener.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.Mode">Mode</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        public static WebServerOptions WithMode(this WebServerOptions @this, HttpListenerMode value)
        {
            @this.Mode = value;
            return @this;
        }

        /// <summary>
        /// Sets the type of HTTP listener to <see cref="HttpListenerMode.EmbedIO"/>.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.Mode">Mode</see> property
        /// set to <see cref="HttpListenerMode.EmbedIO"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        public static WebServerOptions WithEmbedIOHttpListener(this WebServerOptions @this)
        {
            @this.Mode = HttpListenerMode.EmbedIO;
            return @this;
        }

        /// <summary>
        /// Sets the type of HTTP listener to <see cref="HttpListenerMode.Microsoft"/>.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.Mode">Mode</see> property
        /// set to <see cref="HttpListenerMode.Microsoft"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        public static WebServerOptions WithMicrosoftHttpListener(this WebServerOptions @this)
        {
            @this.Mode = HttpListenerMode.Microsoft;
            return @this;
        }

        /// <summary>
        /// Sets the X.509 certificate to use for SSL connections.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="value">The X.509 certificate to use for SSL connections.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.Certificate">Certificate</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        public static WebServerOptions WithCertificate(this WebServerOptions @this, X509Certificate2 value)
        {
            @this.Certificate = value;
            return @this;
        }

        /// <summary>
        /// Sets the thumbprint of the X.509 certificate to use for SSL connections.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="value">The thumbprint of the X.509 certificate to use for SSL connections.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.CertificateThumbprint">CertificateThumbprint</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        public static WebServerOptions WithCertificateThumbprint(this WebServerOptions @this, string value)
        {
            @this.CertificateThumbprint = value;
            return @this;
        }

        /// <summary>
        /// Sets a value indicating whether to automatically load the X.509 certificate.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="value">If <see langword="true"/>, automatically load the X.509 certificate.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.AutoLoadCertificate">AutoLoadCertificate</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="PlatformNotSupportedException"><paramref name="value "/> is <see langword="true"/>
        /// and the underlying operating system is not Windows.</exception>
        public static WebServerOptions WithAutoLoadCertificate(this WebServerOptions @this, bool value)
        {
            @this.AutoLoadCertificate = value;
            return @this;
        }

        /// <summary>
        /// Instructs a <see cref="WebServerOptions"/> instance to automatically load the X.509 certificate.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.AutoLoadCertificate">AutoLoadCertificate</see> property
        /// set to <see langword="true"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="PlatformNotSupportedException">The underlying operating system is not Windows.</exception>
        public static WebServerOptions WithAutoLoadCertificate(this WebServerOptions @this)
        {
            @this.AutoLoadCertificate = true;
            return @this;
        }

        /// <summary>
        /// Instructs a <see cref="WebServerOptions"/> instance to not load the X.509 certificate automatically .
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.AutoLoadCertificate">AutoLoadCertificate</see> property
        /// set to <see langword="false"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        public static WebServerOptions WithoutAutoLoadCertificate(this WebServerOptions @this)
        {
            @this.AutoLoadCertificate = false;
            return @this;
        }

        /// <summary>
        /// Sets a value indicating whether to automatically bind the X.509 certificate
        /// to the port used for HTTPS.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="value">If <see langword="true"/>, automatically bind the X.509 certificate
        /// to the port used for HTTPS.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.AutoRegisterCertificate">AutoRegisterCertificate</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="PlatformNotSupportedException"><paramref name="value "/> is <see langword="true"/>
        /// and the underlying operating system is not Windows.</exception>
        public static WebServerOptions WithAutoRegisterCertificate(this WebServerOptions @this, bool value)
        {
            @this.AutoRegisterCertificate = value;
            return @this;
        }

        /// <summary>
        /// Instructs a <see cref="WebServerOptions"/> instance to automatically bind the X.509 certificate
        /// to the port used for HTTPS.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.AutoRegisterCertificate">AutoRegisterCertificate</see> property
        /// set to <see langword="true"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="PlatformNotSupportedException">The underlying operating system is not Windows.</exception>
        public static WebServerOptions WithAutoRegisterCertificate(this WebServerOptions @this)
        {
            @this.AutoRegisterCertificate = true;
            return @this;
        }

        /// <summary>
        /// Instructs a <see cref="WebServerOptions"/> instance to not bind the X.509 certificate automatically.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.AutoRegisterCertificate">AutoRegisterCertificate</see> property
        /// set to <see langword="false"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        public static WebServerOptions WithoutAutoRegisterCertificate(this WebServerOptions @this)
        {
            @this.AutoRegisterCertificate = false;
            return @this;
        }

        /// <summary>
        /// Sets a value indicating the X.509 certificate store where to load the certificate from.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="value">One of the <see cref="StoreName"/> constants.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.StoreName">StoreName</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="StoreName"/>
        public static WebServerOptions WithStoreName(this WebServerOptions @this, StoreName value)
        {
            @this.StoreName = value;
            return @this;
        }

        /// <summary>
        /// Sets a value indicating the location of the X.509 certificate store where to load the certificate from.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="value">One of the <see cref="StoreLocation"/> constants.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.StoreLocation">StoreLocation</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="StoreLocation"/>
        public static WebServerOptions WithStoreLocation(this WebServerOptions @this, StoreLocation value)
        {
            @this.StoreLocation = value;
            return @this;
        }

        /// <summary>
        /// Sets the name and location of the X.509 certificate store where to load the certificate from.
        /// </summary>
        /// <param name="this">The <see cref="WebServerOptions"/> on which this method is called.</param>
        /// <param name="name">One of the <see cref="StoreName"/> constants.</param>
        /// <param name="location">One of the <see cref="StoreLocation"/> constants.</param>
        /// <returns><paramref name="this"/> with its <see cref="WebServerOptions.StoreName">StoreName</see> property
        /// set to <paramref name="name"/> and its <see cref="WebServerOptions.StoreLocation">StoreLocation</see> property
        /// set to <paramref name="location"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="StoreName"/>
        /// <seealso cref="StoreLocation"/>
        public static WebServerOptions WithStore(this WebServerOptions @this, StoreName name, StoreLocation location)
        {
            @this.StoreName = name;
            @this.StoreLocation = location;
            return @this;
        }
    }
}