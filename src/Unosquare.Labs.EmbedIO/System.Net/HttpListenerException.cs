using System.ComponentModel;
using System.Runtime.InteropServices;

#if !NET452
//------------------------------------------------------------------------------
// <copyright file="HttpListenerException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Net
{
    /// <summary>
    /// Represents an HTTP Listener's exception
    /// </summary>
    /// <seealso cref="System.ComponentModel.Win32Exception" />
    public class HttpListenerException : Win32Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListenerException"/> class.
        /// </summary>
        public HttpListenerException() : base(Marshal.GetLastWin32Error())
        {
            //GlobalLog.Print("HttpListenerException::.ctor() " + NativeErrorCode.ToString() + ":" + Message);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListenerException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        public HttpListenerException(int errorCode) : base(errorCode)
        {
            //GlobalLog.Print("HttpListenerException::.ctor(int) " + NativeErrorCode.ToString() + ":" + Message);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListenerException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        public HttpListenerException(int errorCode, string message) : base(errorCode, message)
        {
            //GlobalLog.Print("HttpListenerException::.ctor(int) " + NativeErrorCode.ToString() + ":" + Message);
        }

        //protected HttpListenerException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        //    : base(serializationInfo, streamingContext)
        //{
        //    //GlobalLog.Print("HttpListenerException::.ctor(serialized) " + NativeErrorCode.ToString() + ":" + Message);
        //}

        /// <summary>
        /// Gets the error code.
        /// </summary>
        /// <value>
        /// The error code.
        /// </value>
        public int ErrorCode => NativeErrorCode;
    }
}
#endif