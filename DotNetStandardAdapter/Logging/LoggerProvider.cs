#region License
/*
 * Copyright (c) Lightstreamer Srl
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion License

namespace Lightstreamer.DotNet.Server.Log
{
    /// <summary>
    /// <para>Simple interface to be implemented to provide custom log consumers to the library.</para>
    /// <para>An instance of the custom implemented class has to be passed to the library through the 
    /// Server.SetLoggerProvider method.</para>
    /// <remarks>
    ///     <para>Exceptions thrown to the caller are not logged.</para>
    ///     <para>Exceptions asynchronously notified to the client are logged at ERROR level.</para>
    ///     <para>All tracing is done at INFO and DEBUG levels.</para>
    ///     <para>Full exception stack traces are logged at DEBUG level.</para>
    /// </remarks>
    /// </summary>
    public interface ILoggerProvider
    {
        /// <summary>
        /// <para>Request for an <see cref="ILogger"/> instance that will be used for logging occuring on the given 
        /// category. It is suggested, but not mandatory, that subsequent calls to this method
        /// related to the same category return the same <see cref="ILogger"/> instance.</para>
        /// </summary>
        /// <param name="category"><para>the log category all messages passed to the given <see cref="ILogger"/> instance will pertain to.
        /// The following categories are used by the library:</para>
        /// <br/>
        ///     <para></para><para>Lightstreamer.DotNet.Server:</para>
        ///         <para>Loggers for Lightstreamer .NET Remote Server and Remote Adapter Library.</para>
        ///     <br/>
        ///     <para></para><para>Lightstreamer.DotNet.Server.ServerMain:</para>
        ///         <para>At INFO level, Remote Server startup is logged;</para>
        ///         <para>At DEBUG level, command line argument recognition is logged.</para>
        ///     <br/>
        ///     <para></para><para>Lightstreamer.DotNet.Server.NetworkedServerStarter:</para> 
        ///         <para>At INFO level, Connection status is logged.</para>
        ///     <br/>
        ///     <para></para><para>Lightstreamer.DotNet.Server.MetadataProviderServer:</para>
        ///         <para>At INFO level, processing options are logged.</para>
        ///         <para>At DEBUG level, processing of requests for the Metadata Adapter is logged.</para>
        ///     <br/>
        ///     <para></para><para>Lightstreamer.DotNet.Server.DataProviderServer:</para>
        ///         <para>At INFO level, processing options are logged.</para>
        ///         <para>At DEBUG level, processing of requests for the Data Adapter is logged.</para>
        ///     <br/>
        ///     <para></para><para>Lightstreamer.DotNet.Server.RequestReply:</para>
        ///         <para>At INFO level, Connection details are logged;</para>
        ///         <para>At DEBUG level, request, reply and notify lines are logged.</para>
        ///     <br/>
        ///     <para></para><para>Lightstreamer.DotNet.Server.RequestReply.Replies.Keepalives:</para>
        ///         <para>At DEBUG level, the keepalives on request/reply streams are logged, so that they can be inhibited.</para>
        ///     <br/>
        ///     <para></para><para>Lightstreamer.DotNet.Server.RequestReply.Notifications:</para>
        ///         <para>At DEBUG level, the notify lines are logged, so that they can be inhibited.</para>
        ///     <br/>
        ///     <para></para><para>Lightstreamer.DotNet.Server.RequestReply.Notifications.Keepalives:</para>
        ///         <para>At DEBUG level, the keepalives on notification streams are logged, so that they can be inhibited.</para>
        ///         <br/>
        /// <para>See also the provided <see href="./app.config"/> sample configuration file.</para>
        ///     
        /// </param>
        /// <returns>
        /// An ILogger instance that will receive log lines related to the given category.
        /// </returns>
        ILogger GetLogger(string category);
    }
}
