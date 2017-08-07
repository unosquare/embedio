using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Net;
using Unosquare.Swan;

namespace Unosquare.Labs.EmbedIO.IoT
{
    class BaseWebApiController : WebApiController
    {
        /// <summary>
        /// Handles the error.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The status code.</param>
        /// <returns>The response</returns>
        protected static bool HandleError(HttpListenerContext context, Exception ex, int statusCode = 500)
        {
            context.Response.StatusCode = statusCode;

            ex.Log(nameof(BaseWebApiController));

            return context.JsonResponse(new
            {
                Title = "Unexpected Error",
                Exception = ex.Stringify(),
            });
        }
    }
}
