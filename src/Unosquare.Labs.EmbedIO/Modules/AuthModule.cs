namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using System.Threading.Tasks;
    using System;
    using System.Linq;
    using System.Collections.Generic;
		using System.Text;
		using System.Net.Http.Headers;
		using System.Security.Principal;
#if NET47
    using System.Net;
#else
    using Net;
#endif
		/**
		 * @brief Basic authentication module. Will return 401 for request if it hasn't authentication header
		 */
    class AuthModule : WebModuleBase
    {
        Dictionary<string, string> accounts = new Dictionary<string, string>();

				/**
				 * @brief Register new account for login
				 *
				 * @Param username account username
				 * @Param password account password
				 *
				 * @Returns   
				 */
        public void AddAccount(string username, string password)
        {
            accounts.Add(username, password);
        }

				/**
				 * @brief adds auth module to server. It will be request authentication header and return 401 if request hasn't
				 *
				 * @Param username basic username
				 * @Param password basic password
				 *
				 * @Returns   
				 */
        public AuthModule(string username, string password)
        {
            AddAccount(username, password);

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

				/**
				 * @brief find authentication field in headers. You can use this method for check any request headers and find user account name
				 *
				 * @Param request
				 *
				 * @Returns pair user-password
				 */
        static public KeyValuePair<string, string> GetAccountData(HttpListenerRequest request)
        {
            var authHeader = request.Headers["Authorization"];
            if (authHeader == null) throw new Exception("Authorization header not found");

            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);

            // RFC 2617 sec 1.2, "scheme" name is case-insensitive
            if (!authHeaderVal.Scheme.Equals("basic",
                    StringComparison.OrdinalIgnoreCase) ||
                authHeaderVal.Parameter == null)
                    throw new Exception("Authorization header not found");

            var encoding = Encoding.GetEncoding("iso-8859-1");
            var credentials = encoding.GetString(Convert.FromBase64String(authHeaderVal.Parameter));

            int separator = credentials.IndexOf(':');
            string name = credentials.Substring(0, separator);
            string password = credentials.Substring(separator + 1);

            return new KeyValuePair<string, string>(name, password);
        }

				/**
				 * @brief checks if headers has authentication matching registered users
				 *
				 * @Param request
				 *
				 * @Returns true if header contains registered account data
				 */
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

        public override string Name => nameof(AuthModule);
    }
}
