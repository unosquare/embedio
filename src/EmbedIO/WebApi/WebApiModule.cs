using System;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// <para>A module using class methods as handlers.</para>
    /// <para>Public instance methods that match the WebServerModule.ResponseHandler signature, and have the WebApi handler attribute
    /// will be used to respond to web server requests.</para>
    /// </summary>
    public class WebApiModule : WebApiModuleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiModule" /> class,
        /// using the default response serializer.
        /// </summary>
        /// <param name="baseRoute">The base URL path served by this module.</param>
        /// <seealso cref="IWebModule.BaseRoute" />
        /// <seealso cref="Validate.UrlPath" />
        public WebApiModule(string baseRoute)
            : base(baseRoute)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiModule" /> class,
        /// using the specified response serializer.
        /// </summary>
        /// <param name="baseRoute">The base URL path served by this module.</param>
        /// <param name="serializer">A <see cref="ResponseSerializerCallback"/> used to serialize
        /// the result of controller methods returning <see langword="object"/>
        /// or <see cref="Task{TResult}">Task&lt;object&gt;</see>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="serializer"/> is <see langword="null"/>.</exception>
        /// <seealso cref="IWebModule.BaseRoute" />
        /// <seealso cref="Validate.UrlPath" />
        public WebApiModule(string baseRoute, ResponseSerializerCallback serializer)
            : base(baseRoute, serializer)
        {
        }

        /// <summary>
        /// <para>Registers a controller type using a constructor.</para>
        /// <para>See <see cref="WebApiModuleBase.RegisterControllerType{TController}()"/>
        /// for further information.</para>
        /// </summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <seealso cref="RegisterController{TController}(Func{TController})"/>
        /// <seealso cref="RegisterController(Type)"/>
        /// <seealso cref="WebApiModuleBase.RegisterControllerType{TController}()"/>
        public void RegisterController<TController>()
            where TController : WebApiController, new()
            => RegisterControllerType(typeof(TController));

        /// <summary>
        /// <para>Registers a controller type using a factory method.</para>
        /// <para>See <see cref="WebApiModuleBase.RegisterControllerType{TController}(Func{TController})"/>
        /// for further information.</para>
        /// </summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <param name="factory">The factory method used to construct instances of <typeparamref name="TController"/>.</param>
        /// <seealso cref="RegisterController{TController}()"/>
        /// <seealso cref="RegisterController(Type,Func{WebApiController})"/>
        /// <seealso cref="WebApiModuleBase.RegisterControllerType{TController}(Func{TController})"/>
        public void RegisterController<TController>(Func<TController> factory)
            where TController : WebApiController
            => RegisterControllerType(typeof(TController), factory);

        /// <summary>
        /// <para>Registers a controller type using a constructor.</para>
        /// <para>See <see cref="WebApiModuleBase.RegisterControllerType(Type)"/>
        /// for further information.</para>
        /// </summary>
        /// <param name="controllerType">The type of the controller.</param>
        /// <seealso cref="RegisterController(Type,Func{WebApiController})"/>
        /// <seealso cref="RegisterController{TController}()"/>
        /// <seealso cref="WebApiModuleBase.RegisterControllerType(Type)"/>
        public void RegisterController(Type controllerType)
            => RegisterControllerType(controllerType);

        /// <summary>
        /// <para>Registers a controller type using a factory method.</para>
        /// <para>See <see cref="WebApiModuleBase.RegisterControllerType(Type,Func{WebApiController})"/>
        /// for further information.</para>
        /// </summary>
        /// <param name="controllerType">The type of the controller.</param>
        /// <param name="factory">The factory method used to construct instances of <paramref name="controllerType"/>.</param>
        /// <seealso cref="RegisterController(Type)"/>
        /// <seealso cref="RegisterController{TController}(Func{TController})"/>
        /// <seealso cref="WebApiModuleBase.RegisterControllerType(Type,Func{WebApiController})"/>
        public void RegisterController(Type controllerType, Func<WebApiController> factory)
            => RegisterControllerType(controllerType, factory);
    }
}