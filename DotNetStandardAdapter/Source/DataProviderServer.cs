/*
 * Copyright (c) 2004-2007 Weswit s.r.l., Via Campanini, 6 - 20124 Milano, Italy.
 * All rights reserved.
 * www.lightstreamer.com
 *
 * This software is the confidential and proprietary information of
 * Weswit s.r.l.
 * You shall not disclose such Confidential Information and shall use it
 * only in accordance with the terms of the license agreement you entered
 * into with Weswit s.r.l.
 */

using System;
using System.IO;
using System.Text;
using System.Collections;

using Lightstreamer.Interfaces.Data;
using Lightstreamer.DotNet.Server.Log;
using Lightstreamer.DotNet.Server.RequestReply;
using Lightstreamer.DotNet.Utils;

namespace Lightstreamer.DotNet.Server {

	/// <summary>
	/// <para>A Remote Server object which can run a Remote Data Adapter and connect it
	/// to a Proxy Data Adapter running on Lightstreamer Server.</para>
	/// <para>The object should be provided with a <see cref="IDataProvider"/> instance
	/// and with suitable local initialization parameters and established connections,
	/// then activated through "Start" and finally disposed through "Stop".
	/// If any preliminary initialization on the supplied <see cref="IDataProvider"/>
	/// implementation object has to be performed, it should be done through a custom,
	/// dedicated method before invoking "Start".
	/// Further reuse of the same instance is not supported.</para>
	/// <para>By default, the invocations to the Data Adapter methods will be
	/// done in dedicated short-lived threads; other options can be specified
	/// in the application configuration file. See the provided
	/// <see href="./app.config"/> sample configuration file for details.</para>
	/// </summary>
	public class DataProviderServer : Server
	{

		private DataProviderServerImpl _impl;

		/// <summary>
		/// Creates an empty server still to be configured and started.
		/// The Init method of the Remote Adapter will be invoked only upon
		/// a Proxy Adapter request.
		/// </summary>
		/// <exception cref="System.Exception"> in case something wrong is
		/// supplied in the application configuration
		/// for Data Adapter processing.
		/// </exception>
		public DataProviderServer()
		{
			_impl = new DataProviderServerImpl();
			init(_impl);
		}

		/// <value>
		/// The Remote Data Adapter instance to be run.
		/// </value>
		public IDataProvider Adapter
		{
			set
			{
				_impl.Adapter = value;
			}
			get
			{
				return _impl.Adapter;
			}
		}

		/// <value>
		/// An IDictionary-type value object to be passed to the Init method
		/// of the Remote Data Adapter, to supply optional parameters. <br/>
		/// The default value is an empty Hashtable. <br/>
		/// See Init in <see cref="IDataProvider"/> for details.
		/// </value>
		public IDictionary AdapterParams
		{
			set
			{
				_impl.AdapterParams = value;
			}
			get
			{
				return _impl.AdapterParams;
			}
		}

		/// <value>
		/// The pathname of an optional configuration file for the Remote
		/// Data Adapter, to be passed to the Init method. <br/>
		/// The default value is null. <br/>
		/// See Init in <see cref="IDataProvider"/> for details.
		/// </value>
		public string AdapterConfig
		{
			set
			{
				_impl.AdapterConfig = value;
			}
			get
			{
				return _impl.AdapterConfig;
			}
		}

	}

	internal class DataProviderServerImpl : ServerImpl, IItemEventListener {
		private static ILog _log= LogManager.GetLogger("Lightstreamer.DotNet.Server.DataProviderServer");

		private bool _initExpected;
		private bool _closeExpected;
		private IDataProvider _adapter;
		private IDictionary _adapterParams;
		private string _adapterConfig;
		private SubscriptionHelper _helper;

        public DataProviderServerImpl() {
			_initExpected = true;
			_closeExpected = true;
				// we start with the current version of the protocol, which does not conflict with earlier versions
			_adapter= null;
			_adapterParams= new Hashtable();
			_adapterConfig= null;
			_helper = new SubscriptionHelper();
        }

		public IDataProvider Adapter {
			set {
				_adapter= value;
			}
			get {
				return _adapter;
			}
		}

		public IDictionary AdapterParams {
			set {
				_adapterParams= value;
			}
			get {
				return _adapterParams;
			}
		}

		public string AdapterConfig {
			set {
				_adapterConfig= value;
			}
			get {
				return _adapterConfig;
			}
		}
		
		public override void Start() {
            _log.Info("Managing Data Adapter " + Name + " with concurrency policy: " + _helper.getConcurrencyPolicy());
            
			base.Start();

			IDictionary credentials = getCredentialParams(_closeExpected);
			if (credentials != null) {
				SendRemoteCredentials(credentials);
			}

			lock (this) {
                if (_notifySender == null)
                    throw new RemotingException("Notification channel not established: can't start (please check that a valid notification TCP port has been specified)");
            }
		}

		private void SendReply(string requestId, string reply) {
			RequestReceiver currRequestReceiver;
			lock (this) {
				currRequestReceiver = _requestReceiver;
			}
			if (currRequestReceiver != null) {
				currRequestReceiver.SendReply(requestId, reply, _log);
			}
		}

		private void SendNotify(string notify) {
			NotifySender currNotifySender;
			lock (this) {
				currNotifySender = _notifySender;
			}
			if (currNotifySender != null) {
				currNotifySender.SendNotify(notify);
			}
		}

		private bool ExecuteSubscribe(SubscribeData data, string requestId) {
            _log.Debug("Processing request: " + requestId);
            string reply = null;
			bool success = false;
			try
			{
				bool snapshotAvailable = _adapter.IsSnapshotAvailable(data.ItemName);
				if (!snapshotAvailable) EndOfSnapshot(data.ItemName);
				_adapter.Subscribe(data.ItemName);
				reply = DataProviderProtocol.WriteSubscribe();
				success = true;
			}
			catch (Exception e)
			{
				reply = DataProviderProtocol.WriteSubscribe(e);
			}

			SendReply(requestId, reply);
			return success;
		}

		private void RefuseLateSubscribe(SubscribeData data, string requestId) {
            _log.Debug("Skipping request: " + requestId);
            Exception e = new SubscriptionException("Subscribe request come too late");
			string reply = DataProviderProtocol.WriteSubscribe(e);
			SendReply(requestId, reply);
		}

		private bool ExecuteUnsubscribe(string itemName, string requestId) {
            _log.Debug("Processing request: " + requestId);
            string reply = null;
			bool success = false;
			try
			{
				_adapter.Unsubscribe(itemName);
				reply = DataProviderProtocol.WriteUnsubscribe();
				success = true;
			}
			catch (Exception e)
			{
				reply = DataProviderProtocol.WriteUnsubscribe(e);
			}
			SendReply(requestId, reply);
			return success;
		}

		private void DummyUnsubscribe(string itemName, string requestId) {
            _log.Debug("Skipping request: " + requestId);
            string reply = DataProviderProtocol.WriteUnsubscribe();
			SendReply(requestId, reply);
		}

		private class SubscriptionTask : MyTask {
			private SubscribeData _data;
			private string _requestId;
			private DataProviderServerImpl _container;
			public SubscriptionTask(DataProviderServerImpl container, SubscribeData data, string requestId)
			{
				_container = container;
				_data = data;
				_requestId = requestId;
			}
			public string getCode() {
				return _requestId;
			}
			public bool DoTask()
			{
				return _container.ExecuteSubscribe(_data, _requestId);
			}
			public void DoLateTask()
			{
                _container.RefuseLateSubscribe(_data, _requestId);
			}
		};

		private class UnsubscriptionTask : MyTask {
			private string _itemName;
			private string _requestId;
			private DataProviderServerImpl _container;
			public UnsubscriptionTask(DataProviderServerImpl container, string itemName, string requestId)
			{
				_container = container;
				_itemName = itemName;
				_requestId = requestId;
			}
			public string getCode() {
				return null;
			}
			public bool DoTask()
			{
                return _container.ExecuteUnsubscribe(_itemName, _requestId);
			}
			public void DoLateTask()
			{
                _container.DummyUnsubscribe(_itemName, _requestId);
			}
		};

        protected void SendRemoteCredentials(IDictionary credentials) {
            String notify = DataProviderProtocol.WriteRemoteCredentials(credentials);
            NotifySender currNotifySender;
            RequestReceiver currRequestReceiver;
            lock (this) {
                currNotifySender = _notifySender;
                currRequestReceiver = _requestReceiver;
            }
            if (currNotifySender != null) {
                currNotifySender.SendNotify(notify);
            }
            if (currRequestReceiver != null) {
                currRequestReceiver.SendUnsolicitedMessage(DataProviderProtocol.AUTH_REQUEST_ID, notify, _log);
            }
        }

		public override void OnRequestReceived(string requestId, string request)
		{
			int sep= request.IndexOf(RemotingProtocol.SEP);
			if (sep < 1) {
				_log.Warn("Discarding malformed request: " + request);
				return;
			}
			
			string method= request.Substring(0, sep);
			
			try {
				if (method.Equals(DataProviderProtocol.METHOD_CLOSE) && _closeExpected) {
					// this can also precede the init request
					if (! requestId.Equals(DataProviderProtocol.CLOSE_REQUEST_ID)) {
						throw new RemotingException("Unexpected id found while parsing a " + DataProviderProtocol.METHOD_CLOSE + " request");
					}
					IDictionary closeParams = DataProviderProtocol.ReadClose(request.Substring(sep + 1));
					String closeReason = (string)closeParams[DataProviderProtocol.KEY_CLOSE_REASON];
					if (closeReason != null) {
						throw new RemotingException("Close requested by the counterpart with reason: " + closeReason);
					} else {
						throw new RemotingException("Close requested by the counterpart");
					}
				}

				bool isInitRequest = method.Equals(DataProviderProtocol.METHOD_DATA_INIT);
				if (isInitRequest && ! _initExpected) {
					throw new RemotingException("Unexpected late " + DataProviderProtocol.METHOD_DATA_INIT + " request");
				} else if (! isInitRequest && _initExpected) {
					throw new RemotingException("Unexpected request " + request + " while waiting for a " + DataProviderProtocol.METHOD_DATA_INIT + " request");
				}

                if (isInitRequest) {
                    _log.Debug("Processing request: " + requestId);
                    _initExpected = false;
                    string keepaliveHint = null;
                    string reply;
                    IDictionary initParams = DataProviderProtocol.ReadInit(request.Substring(sep + 1));
                    try {
                        string proxyVersion = (string)initParams[PROTOCOL_VERSION_PARAM];
                        string advertisedVersion = getSupportedVersion(proxyVersion);
                            // this may prevent the initialization
                        bool is180 = (advertisedVersion == null);
                        bool is182 = (advertisedVersion != null && advertisedVersion.Equals("1.8.2"));

                        if (is180 || is182) {
                            if (_closeExpected) {
                                // WARNING: these versions don't provide for the CLOSE message,
                                // but we previously asked for the CLOSE message with the RAC message;
                                // hence we should no longer expect a CLOSE message, but only after
                                // the client receives this answer, which confirms the protocol;
                                // however, assuming that the Proxy Adapter only supports these versions,
                                // we expect that it has ignored our request in the RAC message,
                                // hence we can already stop expecting a CLOSE message.
                            }
                            _closeExpected = false; 
                        }

                        if (! is180) {
                            // protocol version 1.8.2 and above
                            keepaliveHint = (string)initParams[KEEPALIVE_HINT_PARAM];
                            if (keepaliveHint == null) {
                                keepaliveHint = "0";
                            }
                            initParams.Remove(PROTOCOL_VERSION_PARAM);
                            initParams.Remove(KEEPALIVE_HINT_PARAM);
                                // the version and keepalive hint are internal parameters, not to be sent to the custom Adapter
                        }

                        IEnumerator paramIter = _adapterParams.Keys.GetEnumerator();
                        while (paramIter.MoveNext()) {
                            string param = (string)paramIter.Current;
                            initParams.Add(param, _adapterParams[param]);
                        }
                        _adapter.Init(initParams, _adapterConfig);
                        _adapter.SetListener(this);

                        if (! is180) {
                            // protocol version 1.8.2 and above
                            IDictionary _proxyParams = new Hashtable();
                            _proxyParams.Add(PROTOCOL_VERSION_PARAM, advertisedVersion);
                            reply = DataProviderProtocol.WriteInit(_proxyParams);
                        } else {
                            // protocol version 1.8.0
                            reply = DataProviderProtocol.WriteInit((IDictionary) null);
                        }
                    } catch (Exception e) {
                        reply = DataProviderProtocol.WriteInit(e);
                    }
                    UseKeepaliveHint(keepaliveHint);
                    SendReply(requestId, reply);

				} else if (method.Equals(DataProviderProtocol.METHOD_SUBSCRIBE)) {
					SubscribeData data = DataProviderProtocol.ReadSubscribe(request.Substring(sep + 1));
					MyTask task = new SubscriptionTask(this, data, requestId);
					_helper.DoSubscription(data.ItemName, task);

				} else if (method.Equals(DataProviderProtocol.METHOD_UNSUBSCRIBE)) {
					string itemName = DataProviderProtocol.ReadUnsubscribe(request.Substring(sep + 1));
					MyTask task = new UnsubscriptionTask(this, itemName, requestId);
					_helper.DoUnsubscription(itemName, task);

				} else {
					_log.Warn("Discarding unknown request: " + request);
				}
			
			} catch (Exception e) {
				OnException(e);
			}
		}
		
		public override bool handleIOException(Exception exception) {
			_log.Error("Caught exception: " + exception.Message + ", trying to notify a failure...", exception);
			Exception cause= exception.GetBaseException();
			_log.Fatal("Exception caught while reading/writing from/to streams: '" + cause.Message + "', aborting...", cause);

			System.Environment.Exit(0);
			return false;
		}

		public override bool handleException(Exception exception) {
			_log.Error("Caught exception: " + exception.Message + ", trying to notify a failure...", exception);

			try {
				string notify= DataProviderProtocol.WriteFailure(exception);
				SendNotify(notify);
			} 
			catch (Exception e2) {
				_log.Error("Caught second-level exception while trying to notify a first-level exception: " + e2.Message, e2);
			}
			return false;
		}

		// ////////////////////////////////////////////////////////////////////////
		// IItemEventListener methods
		
		public void Update(string itemName, IItemEvent itemEvent, bool isSnapshot) {
			// both GetSubscriptionCode and SendNotify take simple locks,
			// which don't block and don't take further locks;
			// hence this invocation can be made by the Adapter while holding
			// the lock on the item state, with no issues
			string code = _helper.GetSubscriptionCode(itemName);
			if (code != null) {
				try {
					string notify= DataProviderProtocol.WriteUpdateByEvent(itemName, code, itemEvent, isSnapshot);
					SendNotify(notify);
			
				} catch (Exception e) {
					OnException(e);
				}
			} else {
				// there is no active subscription in this moment;
				// this must be an error by the Adapter, which must have sent
				// the event after the termination of an Unsubscribe()
				// (or before, but without synchronizing, which is also wrong)
				_log.Warn("Unexpected update for item " + itemName);
			}
		}
		
		public void Update(string itemName, IDictionary itemEvent, bool isSnapshot) {
			// both GetSubscriptionCode and SendNotify take simple locks,
			// which don't block and don't take further locks;
			// hence this invocation can be made by the Adapter while holding
			// the lock on the item state, with no issues
			string code = _helper.GetSubscriptionCode(itemName);
			if (code != null) {
				try {
					string notify = DataProviderProtocol.WriteUpdateByMap(itemName, code, itemEvent, isSnapshot);
					SendNotify(notify);
			
				} catch (Exception e) {
					OnException(e);
				}
			} else {
				// there is no active subscription in this moment;
				// this must be an error by the Adapter, which must have sent
				// the event after the termination of an Unsubscribe()
				// (or before, but without synchronizing, which is also wrong)
				_log.Warn("Unexpected update for item " + itemName);
			}
		}
		
		public void Update(string itemName, IIndexedItemEvent itemEvent, bool isSnapshot) {
			// both GetSubscriptionCode and SendNotify take simple locks,
			// which don't block and don't take further locks;
			// hence this invocation can be made by the Adapter while holding
			// the lock on the item state, with no issues
			string code = _helper.GetSubscriptionCode(itemName);
			if (code != null) {
				try
				{
					string notify = DataProviderProtocol.WriteUpdateByIndexedEvent(itemName, code, itemEvent, isSnapshot);
					SendNotify(notify);

				}
				catch (Exception e)
				{
					OnException(e);
				}
			} else {
				// there is no active subscription in this moment;
				// this must be an error by the Adapter, which must have sent
				// the event after the termination of an Unsubscribe()
				// (or before, but without synchronizing, which is also wrong)
				_log.Warn("Unexpected update for item " + itemName);
			}
		}

		public void EndOfSnapshot(string itemName) {
			// both GetSubscriptionCode and SendNotify take simple locks,
			// which don't block and don't take further locks;
			// hence this invocation can be made by the Adapter while holding
			// the lock on the item state, with no issues
			string code = _helper.GetSubscriptionCode(itemName);
			if (code != null) {
				try {
					string notify = DataProviderProtocol.WriteEndOfSnapshot(itemName, code);
					SendNotify(notify);
			
				} catch (Exception e) {
					OnException(e);
				}
			} else {
				// there is no active subscription in this moment;
				// this must be an error by the Adapter, which must have sent
				// the event after the termination of an Unsubscribe()
				// (or before, but without synchronizing, which is also wrong)
				_log.Warn("Unexpected end of snapshot notify for item " + itemName);
			}
		}

        public void ClearSnapshot(string itemName) {
			// both GetSubscriptionCode and SendNotify take simple locks,
			// which don't block and don't take further locks;
			// hence this invocation can be made by the Adapter while holding
			// the lock on the item state, with no issues
			string code = _helper.GetSubscriptionCode(itemName);
            if (code != null)
            {
                try
                {
                    string notify = DataProviderProtocol.WriteClearSnapshot(itemName, code);
					SendNotify(notify);

                }
                catch (Exception e)
                {
                    OnException(e);
                }
            }
            else
            {
				// there is no active subscription in this moment;
				// this must be an error by the Adapter, which must have sent
				// the event after the termination of an Unsubscribe()
				// (or before, but without synchronizing, which is also wrong)
				_log.Warn("Unexpected clear snapshot request for item " + itemName);
            }
        }

		public void Failure(Exception exception) {
			try {
				string notify= DataProviderProtocol.WriteFailure(exception);
				SendNotify(notify);
		
			} catch (Exception e2) {
				OnException(e2);
			}
		}

	}

}
