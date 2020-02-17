using System;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// Provides extension methods for <see cref="WebApiModule"/>.
    /// </summary>
    public static class WebApiModuleExtensions
    {
        /// <summary>
        /// <para>Registers a controller type using a constructor.</para>
        /// <para>See <see cref="WebApiModuleBase.RegisterControllerType{TController}()"/>
        /// for further information.</para>
        /// </summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <param name="this">The <see cref="WebApiModule"/> on which this method is called.</param>
        /// <returns><paramref name="this"/> with the controller type registered.</returns>
        /// <seealso cref="WithController{TController}(WebApiModule,Func{TController})"/>
        /// <seealso cref="WithController(WebApiModule,Type)"/>
        /// <seealso cref="WebApiModuleBase.RegisterControllerType{TController}()"/>
        public static WebApiModule WithController<TController>(this WebApiModule @this)
            where TController : WebApiController, new()
        {
            @this.RegisterController<TController>();
            return @this;
        }

        /// <summary>
        /// <para>Registers a controller type using a factory method.</para>
        /// <para>See <see cref="WebApiModuleBase.RegisterControllerType{TController}(Func{TController})"/>
        /// for further information.</para>
        /// </summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <param name="this">The <see cref="WebApiModule"/> on which this method is called.</param>
        /// <param name="factory">The factory method used to construct instances of <typeparamref name="TController"/>.</param>
        /// <returns><paramref name="this"/> with the controller type registered.</returns>
        /// <seealso cref="WithController{TController}(WebApiModule)"/>
        /// <seealso cref="WithController(WebApiModule,Type,Func{WebApiController})"/>
        /// <seealso cref="WebApiModuleBase.RegisterControllerType{TController}(Func{TController})"/>
        public static WebApiModule WithController<TController>(this WebApiModule @this, Func<TController> factory)
            where TController : WebApiController
        {
            @this.RegisterController(factory);
            return @this;
        }

        /// <summary>
        /// <para>Registers a controller type using a constructor.</para>
        /// <para>See <see cref="WebApiModuleBase.RegisterControllerType(Type)"/>
        /// for further information.</para>
        /// </summary>
        /// <param name="this">The <see cref="WebApiModule"/> on which this method is called.</param>
        /// <param name="controllerType">The type of the controller.</param>
        /// <returns><paramref name="this"/> with the controller type registered.</returns>
        /// <seealso cref="WithController(WebApiModule,Type,Func{WebApiController})"/>
        /// <seealso cref="WithController{TController}(WebApiModule)"/>
        /// <seealso cref="WebApiModuleBase.RegisterControllerType(Type)"/>
        public static WebApiModule WithController(this WebApiModule @this, Type controllerType)
        {
            @this.RegisterController(controllerType);
            return @this;
        }

        /// <summary>
        /// <para>Registers a controller type using a factory method.</para>
        /// <para>See <see cref="WebApiModuleBase.RegisterControllerType(Type,Func{WebApiController})"/>
        /// for further information.</para>
        /// </summary>
        /// <param name="this">The <see cref="WebApiModule"/> on which this method is called.</param>
        /// <param name="controllerType">The type of the controller.</param>
        /// <param name="factory">The factory method used to construct instances of <paramref name="controllerType"/>.</param>
        /// <returns><paramref name="this"/> with the controller type registered.</returns>
        /// <seealso cref="WithController(WebApiModule,Type)"/>
        /// <seealso cref="WithController{TController}(WebApiModule,Func{TController})"/>
        /// <seealso cref="WebApiModuleBase.RegisterControllerType(Type,Func{WebApiController})"/>
        public static WebApiModule WithController(this WebApiModule @this, Type controllerType, Func<WebApiController> factory)
        {
            @this.RegisterController(controllerType, factory);
            return @this;
        }
    }
}