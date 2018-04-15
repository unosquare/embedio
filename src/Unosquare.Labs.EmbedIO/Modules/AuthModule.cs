namespace Unosquare.Labs.EmbedIO.Modules
{
	using Constants;
    using System;
    using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
    using System.Collections.Generic;
#if NET47
    using System.Net;
#else
    using Net;
#endif

      /// <summary>
    /// Simple authorisation module that requests http auth from client
    /// Will return 401 + WWW-Authenticate header if request isn't authorised
    /// </summary>
    class AuthModule : WebModuleBase
    {
        /// <summary>
        /// List of registred accounts. User-Password pair
        /// </summary>
        Dictionary<string, string> accounts = new Dictionary<string, string>();

        /// <summary>
        /// Add new account
        /// </summary>
        /// <param name="username">account username</param>
        /// <param name="password">account password</param>
        public void AddAccount(string username, string password)
        {
            accounts.Add(username, password);
        }

        /// <summary>
        /// Construct with one registered account
        /// </summary>
        /// <param name="username">account username</param>
        /// <param name="password">account password</param>
        public AuthModule(string username, string password) : this()
        {
            AddAccount(username, password);
        }

        /// <summary>
        /// Constructor. Use AddAccount(user, password) after that if you want to connect somehow
        /// </summary>
        public AuthModule()
        {
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (context, ct) =>
            {
                try
                {
                    if (!IsAuthorized(context.Request))
                        context.Response.StatusCode = 401;
                }
                catch (FormatException)
                {
                    // Credentials were not formatted correctly.
                    context.Response.StatusCode = 401;
                }

                if (context.Response.StatusCode == 401)
                {
                    context.Response.Headers.Add("WWW-Authenticate",
                        string.Format("Basic realm=\"{0}\"", "Realm"));

                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            });
        }

        /// <summary>
        /// Parses request for account data
        /// </summary>
        /// <param name="request">HttpListenerRequest</param>
        /// <returns>user-password KeyValuePair from request</returns>
        /// <exception>
        /// if request isn't authorised
        /// </exception>
        static public KeyValuePair<string, string> GetAccountData(HttpListenerRequest request)
        {
            var authHeader = request.Headers["Authorization"];
            if (authHeader == null) throw new Exception("Authorization header not found");

            // RFC 2617 sec 1.2, "scheme" name is case-insensitive
            // header contains name and parameter separated by space. If it equals just "basic" - it's empty
            if (!authHeader.Equals("basic",
                    StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Authorization header not found");

            var encoding = Encoding.GetEncoding("iso-8859-1");
            var credentials = encoding.GetString(Convert.FromBase64String(authHeader.Split(' ')[1]));

            int separator = credentials.IndexOf(':');
            string name = credentials.Substring(0, separator);
            string password = credentials.Substring(separator + 1);

            return new KeyValuePair<string, string>(name, password);
        }

        /// <summary>
        /// Validates request and returns true if that account data registred in this module and request has auth data  
        /// </summary>
        /// <param name="request">HttpListenerRequest</param>
        /// <returns>
        /// true if request authorised
        /// </returns>
        public bool IsAuthorized(HttpListenerRequest request)
        {
            try
            {
                var data = GetAccountData(request);
                if (!accounts.TryGetValue(data.Key, out string password) || password != data.Value)
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override string Name => nameof(AuthModule);

    }
}
