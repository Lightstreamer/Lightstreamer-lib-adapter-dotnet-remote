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

using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Collections;

using Lightstreamer.Interfaces.Data;
using Lightstreamer.DotNet.Server.Log;
using Lightstreamer.DotNet.Server.RequestReply;

namespace Lightstreamer.DotNet.Server {

	/// <summary>
	/// <para>To be implemented in order to provide a Remote Server instance with
	/// a custom handler for error conditions occurring on the Remote Server.</para>
	/// <para>Note that multiple redundant invocations on the same Remote Server
	/// instance are possible.</para>
	/// </summary>
	public interface IExceptionHandler {

		/// <summary>
		/// <para>Called by the Remote Server upon a read or write operation
		/// failure. This may mean that the connection to Lightstreamer Server
		/// is lost; in any way, after this error, the correct operation
		/// of this Remote Server instance is compromised.
		/// This may be the signal of a normal termination of Lightstreamer Server.
		/// If this is not the case, then this Remote Server instance should be closed
		/// and a new one should be created and initialized. This may mean
		/// closing and restarting the process or just creating a new instance,
		/// depending on the implementation choice. This will be
		/// detected by the Proxy Adapter, which will react accordingly.</para>
		/// <para>The default handling just terminates the process.</para>
		/// </summary>
		/// <param name="exception">An Exception showing the cause of the
		/// problem.</param>
		/// <returns>true to enable the default handling, false to suppress it.</returns>
		bool handleIOException(Exception exception);

		/// <summary>
		/// <para>Called by the Remote Server upon an unexpected error.
		/// After this error, the correct operation of this Remote Server
		/// instance is compromised.
		/// If this is the case, then this Remote Server instance should be closed
		/// and a new one should be created and initialized. This may mean
		/// closing and restarting the process or just creating a new instance,
		/// depending on the implementation choice. This will be
		/// detected by the Proxy Adapter, which will react accordingly.</para>
		/// <para>The default handling, in case of a Remote Data Adapter,
		/// issues an asynchronous failure notification to the Proxy Adapter.
		/// In case of a Remote Metadata Adapter, the default handling ignores
		/// the notification; however, as a consequence of the Remote Protocol
		/// being broken, the Proxy Adapter may return exceptions against
		/// one or more specific requests by Lightstreamer Kernel.</para>
		/// </summary>
		/// <param name="exception">An Exception showing the cause of the
		/// problem.</param>
		/// <returns>true to enable the default handling, false to suppress it.</returns>
		bool handleException(Exception exception);

	}

	/// <summary>
	/// <para>A generic Remote Server object, which can run a Remote Data or Metadata Adapter
	/// and connect it to the Proxy Adapter running on Lightstreamer Server.</para>
	/// <para>The object should be provided with a suitable Adapter instance
	/// and with suitable local initialization parameters and established connections,
	/// then activated through "Start" and finally disposed through "Stop".
	/// If any preliminary initialization on the supplied Adapter implementation
	/// object has to be performed, it should be done through a custom,
	/// dedicated method before invoking "Start".
	/// Further reuse of the same instance is not supported.</para>
	/// <para>Some initialization parameters can be specified
	/// in the application configuration file. See the provided
	/// <see href="./app.config"/> sample configuration file for details.</para>
	/// </summary>
	public abstract class Server
	{

		private ServerImpl _impl;
        private bool startedOnce = false;

		internal void init(ServerImpl impl)
		{
            _impl = impl;
		}

		/// <value>
		/// A name for the Server instance; used for logging purposes. 
		/// </value>
		public string Name
		{
			set
			{
                if (startedOnce)
                {
                    throw new RemotingException("Reuse of Server object forbidden");
                }
				_impl.Name = value;
			}
			get
			{
				return _impl.Name;
			}
        }

		/// <value>
		/// <para>
		/// The user-name credential to be sent to the Proxy Adapter upon connection.
		/// The credentials are needed only if the Proxy Adapter is configured
		/// to require Remote Adapter authentication.</para>
		/// <para>The default value is null.</para>
		/// </value>
		public string RemoteUser
        {
            set
            {
                _impl.RemoteUser = value;
            }
            get
            {
                return _impl.RemoteUser;
            }
        }

        /// <value>
        /// <para>
        /// The password credential to be sent to the Proxy Adapter upon connection.
        /// The credentials are needed only if the Proxy Adapter is configured
        /// to require Remote Adapter authentication.</para>
        /// <para>The default value is null.</para>
        /// </value>
        public string RemotePassword
        {
            set
            {
                _impl.RemotePassword = value;
            }
            get
            {
                return _impl.RemotePassword;
            }
        }

        /// <value>
        /// The stream used by the Proxy Adapter in order to forward the requests
        /// to the Remote Adapter. 
        /// </value>
        public Stream RequestStream
		{
			set
			{
                if (startedOnce)
                {
                    throw new RemotingException("Reuse of Server object forbidden");
                }
                _impl.RequestStream = value;
			}
			get
			{
				return _impl.RequestStream;
			}
		}

		/// <value>
		/// The stream used by the Remote Adapter in order to forward the answers
		/// to the Proxy Adapter. 
		/// </value>
		public Stream ReplyStream
		{
			set
			{
                if (startedOnce)
                {
                    throw new RemotingException("Reuse of Server object forbidden");
                }
                _impl.ReplyStream = value;
			}
			get
			{
				return _impl.ReplyStream;
			}
		}

		/// <value>
		/// A handler for error conditions occurring on the Remote Server.
		/// By setting the handler, it's possible to override the default
		/// exception handling.
		/// </value>
		public IExceptionHandler ExceptionHandler
		{
			set
			{
				_impl.ExceptionHandler = value;
			}
			get
			{
				return _impl.ExceptionHandler;
			}
		}

		/// <summary>
		/// Starts the communication between the Remote Adapter and the Proxy Adapter
		/// through the supplied streams.
		/// Then, requests issued by the Proxy Adapter are received and forwarded
		/// to the Remote Adapter. Note that the Remote Adapter initialization
		/// is not done now, but it will be triggered by the Proxy Adapter
		/// and any initialization error will be just notified to the Proxy Adapter.
		/// </summary>
		/// <exception cref="System.Exception">An error occurred in the initialization
		/// phase. The adapter was not started.
		/// </exception>
		public void Start()
		{
            if (startedOnce)
            {
                throw new RemotingException("Reuse of Server object forbidden");
            }
            startedOnce = true;
            try
            {
				_impl.Start();
			} 
			catch (Exception e) {
				_impl.Stop();
				throw e;
			}
		}

		/// <summary>
		/// <para>Stops the management of the Remote Adapter and destroys
		/// the threads used by this Server. This instance can no longer
		/// be used.</para>
        /// <para>The streams supplied to this instance and the associated
        /// sockets are also closed.</para>
		/// <para>Note that this does not stop the supplied Remote Adapter,
        /// as no close method is available in the Remote Adapter interface.
        /// If the process is not terminating, then the Remote Adapter
        /// cleanup should be performed by accessing the supplied Adapter
        /// instance directly and calling custom methods.</para>
		/// </summary>
		public void Close()
		{
			_impl.Stop();
            _impl.Dispose();
            _impl = null;
		}

		/// <summary>
		/// <para>Sets the <see cref="ILoggerProvider"/> instance that will be used by the classes of the library to obtain <see cref="ILogger"/> instances
		/// used to propagate internal logging. Providing a new provider to the library permits to consume the log produced through
		/// custom ILogger implementation.</para>
		/// <para>As soon as a new <see cref="ILoggerProvider"/> is provided all the instances of <see cref="ILogger"/> already in use in the
		/// library are discarded and substituted with instances obtained from this new object. If a null value is provided,
		/// the default consumers, that discard all the log, are enabled.</para>
		/// <para>The following categories are available to be consumed:</para>
		/// <para>Lightstreamer.DotNet.Server: with various subloggers, logs the activity of this class and its subclasses;
		/// at INFO level, adapter lifecycle events are logged; at DEBUG level, request handling is logged.</para>
		/// <para>Lightstreamer.DotNet.RequestReply: with various subloggers, logs the message exchange activity;
		/// at INFO level, connection handling lifecycle events are logged; at DEBUG level, inbound and outbound message details are logged.</para>
		/// </summary>
		/// <param name="loggerProvider">Will be responsible to provide <see cref="ILogger"/> instances to the various classes of the library.</param>
		public static void SetLoggerProvider(ILoggerProvider loggerProvider)
        {
            LogManager.SetLoggerProvider(loggerProvider);
        }
    
    }

	internal abstract class ServerImpl : IRequestListener, IExceptionListener, IExceptionHandler {
		private static ILog _log = LogManager.GetLogger("Lightstreamer.DotNet.Server");

		private static int _number = 0;

		private string _name;

		protected string _maxVersion = "1.9.1";

		private Stream _requestStream;
		private Stream _replyStream;

		private int? _configuredKeepaliveMillis;

        private string _remoteUser;
        private string _remotePassword;

        public static string PROTOCOL_VERSION_PARAM = "ARI.version";
        public static string KEEPALIVE_HINT_PARAM = "keepalive_hint.millis";
        public static string USER_PARAM = "user";
        public static string PASSWORD_PARAM = "password";
		public static string OUTCOME_PARAM = "enableClosePacket";
		public static string SDK_PARAM = "SDK";
		public static string SDK_NAME = ".Net Standard Adapter SDK";

		public static int MIN_KEEPALIVE_MILLIS = 1000;
			// protection limit; it might be made configurable;
			// in that case, if 0 is allowed to suppress keepalives, its handling should be added
		public static int STRICT_KEEPALIVE_MILLIS = 1000;
			// default to act on both intermediate nodes and the Proxy Adapter,
			// when the Proxy Adapter provides no information
		public static int DEFAULT_KEEPALIVE_MILLIS = 10000;
			// default to act on intermediate nodes, not on the Proxy Adapter

		private IExceptionHandler _exceptionHandler;
		
		protected RequestManager _requestManager;
		protected MessageSender _notifySender;

		public ServerImpl() {
			_number++;
			_name= "#" + _number;

			_requestStream= null;
			_replyStream= null;
			
			_exceptionHandler = null;

			_requestManager= null;
			_notifySender= null;

            System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
            string keepaliveConf = appSettings["Lightstreamer.Keepalive.Millis"];
            if (keepaliveConf != null) {
                try {
                    _configuredKeepaliveMillis = int.Parse(keepaliveConf);
                    if (_configuredKeepaliveMillis < 0) {
                        _configuredKeepaliveMillis = 0;
                    }
                } catch (Exception) {
                    throw new Exception("Invalid Lightstreamer.Keepalive.Seconds configuration: " + keepaliveConf);
                }
            } else {
                _configuredKeepaliveMillis = null;
            }
		}

		public string Name {
			set {
				_name= value;
			}
			get {
				return _name;
			}
		}

		public string RemoteUser {
			set {
				_remoteUser= value;
			}
			get {
				return _remoteUser;
			}
		}

		public string RemotePassword {
			set {
				_remotePassword= value;
			}
			get {
				return _remotePassword;
			}
		}

		public Stream RequestStream {
			set {
				_requestStream= value;
			}
			get {
				return _requestStream;
			}
		}

		public Stream ReplyStream {
			set {
				_replyStream= value;
			}
			get {
				return _replyStream;
			}
		}

		public IExceptionHandler ExceptionHandler {
			set {
				_exceptionHandler = value;
			}
			get {
				return _exceptionHandler;
			}
		}

        protected virtual string getSupportedVersion(string proxyVersion) {
			Debug.Assert(_maxVersion.Equals("1.9.1")); // to be kept aligned when upgrading

			if (proxyVersion == null) {
				// protocol version 1.8.0 or earlier, not supported
				throw new Exception("Unsupported protocol version");
			}
			else {
                // protocol version specified in proxyVersion (must be 1.8.2 or later);
                // if we supported a lower version, we could advertise it
                // and hope that the proxy supports it as well;
                // if we supported a higher version, we could fail here,
                // but we can still advertise it and let the proxy refuse
				if (proxyVersion.StartsWith("1.8.")) {
	                _log.Info("Received Proxy Adapter protocol version as " + proxyVersion + " for " + _name + ": no longer supported.");
					throw new Exception("Unsupported protocol version");
				} else if (proxyVersion.Equals("1.9.0")) {
					Debug.Assert(false); // must have been handled by the subclass
					return null;
				} else if (proxyVersion.Equals(_maxVersion)) {
					_log.Info("Received Proxy Adapter protocol version as " + proxyVersion + " for " + _name + ": versions match.");
					return _maxVersion;
				} else {
					_log.Info("Received Proxy Adapter protocol version as " + proxyVersion + " for " + _name + ": requesting " + _maxVersion + ".");
					return _maxVersion;
				}
            }
        }

        protected IDictionary getCredentialParams(bool requestOutcome) {
            IDictionary _proxyParams = new Hashtable();
            if (_remoteUser != null) {
                _proxyParams.Add(USER_PARAM, _remoteUser);
            }
            if (_remotePassword != null) {
                _proxyParams.Add(PASSWORD_PARAM, _remotePassword);
            }
            if (requestOutcome) {
                _proxyParams.Add(OUTCOME_PARAM, "true");
            }
			_proxyParams.Add(SDK_PARAM, SDK_NAME);
			return _proxyParams;
        }

		private void ChangeKeepalive(int keepaliveTime) {
			MessageSender currNotifySender;
			RequestManager currRequestManager;
			lock(this) {
				currNotifySender = _notifySender;
				currRequestManager = _requestManager;
			}
			if (currNotifySender != null) {
				currNotifySender.ChangeKeepalive(keepaliveTime, true);
			}
			if (currRequestManager != null) {
				currRequestManager.ChangeKeepalive(keepaliveTime, false);
				// interruption not needed, since in this context we are about to reply
			}
		}

		protected void UseKeepaliveHint(String keepaliveHint) {
			if (keepaliveHint == null) {
				// no information: we stick to a stricter default
				if (_configuredKeepaliveMillis == null) {
					// we had temporarily set the default, but we have to set a final value in any case
					_log.Info("Keepalive time for " + _name + " finally set to " + STRICT_KEEPALIVE_MILLIS + " milliseconds" +
							" to support old Proxy Adapter");
					ChangeKeepalive(STRICT_KEEPALIVE_MILLIS);
				} else {
					// for backward compatibility we keep the setting;
					// it is possible that the setting is too long
					// and the Proxy Adapter activity check is triggered
				}
			} else {
				int keepaliveTime = int.Parse(keepaliveHint);
				if (keepaliveTime <= 0) {
					// no restrictions, so our default is still meaningful
				} else if (_configuredKeepaliveMillis == null) {
					// we had temporarily set the default, but we have to set a final value in any case
					if (keepaliveTime < DEFAULT_KEEPALIVE_MILLIS) {
						if (keepaliveTime >= MIN_KEEPALIVE_MILLIS) {
							_log.Info("Keepalive time for " + _name + " finally set to " + keepaliveTime + " milliseconds" +
									" as per Proxy Adapter suggestion");
							ChangeKeepalive(keepaliveTime);
						} else {
							_log.Warn("Keepalive time for " + _name + " finally set to " + MIN_KEEPALIVE_MILLIS + "milliseconds" +
									" , despite a Proxy Adapter suggestion of " + keepaliveTime + " milliseconds");
							ChangeKeepalive(MIN_KEEPALIVE_MILLIS);
						}
					} else {
						// the default setting is stricter, so it's ok
						_log.Info("Keepalive time for " + _name + " finally confirmed to " + DEFAULT_KEEPALIVE_MILLIS + " milliseconds" +
								" consistently with Proxy Adapter suggestion");
					}
				} else if (_configuredKeepaliveMillis > 0) {
					// we had set the configured value, but we may have to change it
					if (keepaliveTime < _configuredKeepaliveMillis) {
						if (keepaliveTime >= MIN_KEEPALIVE_MILLIS) {
							_log.Warn("Keepalive time for " + _name + " changed to " + keepaliveTime + " milliseconds" +
									" as per Proxy Adapter suggestion");
							ChangeKeepalive(keepaliveTime);
						} else {
							_log.Warn("Keepalive time for " + _name + " changed to " + MIN_KEEPALIVE_MILLIS + "milliseconds" +
									" , despite a Proxy Adapter suggestion of " + keepaliveTime + " milliseconds");
							ChangeKeepalive(MIN_KEEPALIVE_MILLIS);
						}
					} else {
						// our setting is stricter, so it's ok
					}
				} else {
					// we hadn't used the keepalive, but we may have to enforce them
					if (keepaliveTime >= MIN_KEEPALIVE_MILLIS) {
						_log.Warn("Keepalives for " + _name + " forced with time " + keepaliveTime + " milliseconds" +
								" as per Proxy Adapter suggestion");
						ChangeKeepalive(keepaliveTime);
					} else {
						_log.Warn("Keepalives for " + _name + " forced with time " + MIN_KEEPALIVE_MILLIS + "milliseconds" +
								" , despite a Proxy Adapter suggestion of " + keepaliveTime + " milliseconds");
						ChangeKeepalive(MIN_KEEPALIVE_MILLIS);
					}
				}
			}
		}

		abstract public void Start();

		public void Init(bool withNotifies) {
			_log.Info("Remote Adapter " + _name + " starting with protocol version " + _maxVersion);
			int keepaliveMillis;
			if (_configuredKeepaliveMillis == null) {
				keepaliveMillis = DEFAULT_KEEPALIVE_MILLIS;
				_log.Info("Keepalive time for " + _name + " temporarily set to " + keepaliveMillis + " milliseconds");
			} else if (_configuredKeepaliveMillis > 0) {
				keepaliveMillis = (int) _configuredKeepaliveMillis;
				_log.Info("Keepalive time for " + _name + " set to " + keepaliveMillis + " milliseconds");
			} else {
				keepaliveMillis = 0;
				_log.Info("Keepalives for " + _name + " not set");
			}

			WriteState sharedWriteState;
			if (withNotifies) {
				sharedWriteState = new WriteState();
			} else {
				sharedWriteState = null;
			}
			
			RequestManager currRequestManager = null;
            currRequestManager = new RequestManager(_name, _requestStream, _replyStream, sharedWriteState, keepaliveMillis, this, this);

            MessageSender currNotifySender = null;
            if (withNotifies) currNotifySender = new MessageSender(_name, _replyStream, sharedWriteState, keepaliveMillis, this);

            lock (this) {
                _notifySender = currNotifySender;
                _requestManager = currRequestManager;
			}
        }

        public void StartOut() {
            RequestManager currRequestManager;
            MessageSender currNotifySender;
            lock (this) {
                currRequestManager = _requestManager;
                currNotifySender = _notifySender;
            }
			if (currNotifySender != null) {
				currNotifySender.StartOut();
			}
            if (currRequestManager != null) {
				currRequestManager.StartOut();
			}
		}

		public void StartIn() {
            RequestManager currRequestManager;
            lock (this) {
                currRequestManager = _requestManager;
            }
            if (currRequestManager != null) {
				currRequestManager.StartIn();
			}
		}

		public void Stop() {
            RequestManager currRequestManager;
            MessageSender currNotifySender;
            lock (this) {
                currRequestManager = _requestManager;
			    _requestManager = null;
                currNotifySender = _notifySender;
                _notifySender = null;
            }

            if (currRequestManager != null) {
                try { currRequestManager.Quit(); } 
			    catch (Exception) {}
		    }
            if (currNotifySender != null) {
                try { currNotifySender.Quit(); } 
			    catch (Exception) {}
		    }
		}

        public void Dispose() {
			if (_requestStream != null) {
				try { _requestStream.Close(); }
				catch (Exception) { }
			}
			if (_replyStream != null) {
				try { _replyStream.Close(); }
				catch (Exception) { }
			}

			_requestStream = null;
            _replyStream = null;
        }

		public abstract bool handleIOException(Exception exception);
		public abstract bool handleException(Exception exception);

		public abstract void OnRequestReceived(string requestId, string request);

		public void OnException(Exception exception) {
			if (exception is RemotingException) {
				Exception cause= exception.GetBaseException();
				if ((cause is IOException) || (cause is SocketException)) {
					if (_exceptionHandler != null) {
						_log.Info("Caught exception: " + exception.ToString() + ", notifying the application...");
						if (! _exceptionHandler.handleIOException(exception)) {
							return;
						}
					}
					handleIOException(exception);
					return;
				}
			} 

			if (_exceptionHandler != null) {
				_log.Info("Caught exception: " + exception.ToString() + ", notifying the application...");
				if (! _exceptionHandler.handleException(exception)) {
					return;
				}
			}
			handleException(exception);
		}
	}
	
}
