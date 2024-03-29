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
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using System.Threading.Tasks;

using Lightstreamer.Interfaces.Metadata;
using Lightstreamer.DotNet.Server.Log;
using Lightstreamer.DotNet.Server.RequestReply;

namespace Lightstreamer.DotNet.Server {

	/// <summary>
	/// <para>A Remote Server object which can run a Remote Metadata Adapter and connect it
	/// to a Proxy Metadata Adapter running on Lightstreamer Server.</para>
	/// <para>The object should be provided with a <see cref="IMetadataProvider"/> instance
	/// and with suitable local initialization parameters and established connections,
	/// then activated through "Start" and finally disposed through "Stop".
	/// If any preliminary initialization on the supplied <see cref="IMetadataProvider"/>
	/// implementation object has to be performed, it should be done through a custom,
	/// dedicated method before invoking "Start".
	/// Further reuse of the same instance is not supported.</para>
	/// <para>By default, the invocations to the Metadata Adapter methods
	/// will be done in the System Thread Pool; other options can be specified
	/// in the application configuration file. See the provided
	/// <see href="./app.config"/> sample configuration file for details.</para>
	/// </summary>
	public class MetadataProviderServer : Server
	{

		private MetadataProviderServerImpl _impl;

		/// <summary>
		/// Creates an empty server still to be configured and started.
		/// The Init method of the Remote Adapter will be invoked only upon
		/// a Proxy Adapter request.
		/// </summary>
		/// <exception cref="System.Exception">
		/// in case something wrong is supplied in the application configuration
		/// for Metadata Adapter processing.
		/// </exception>
		public MetadataProviderServer()
		{
			_impl = new MetadataProviderServerImpl();
			init(_impl);
		}

		/// <value>
		/// The Remote Metadata Adapter instance to be run.
		/// </value>
		public IMetadataProvider Adapter
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
		/// of the Remote Metadata Adapter, to supply optional parameters.<br/>
		/// The default value is an empty Hashtable.<br/>
		/// See Init in <see cref="IMetadataProvider"/> for details.
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
		/// Metadata Adapter, to be passed to the Init method.<br/>
        /// The default value is null.<br/>
		/// See Init in <see cref="IMetadataProvider"/> for details.
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

    internal class MetadataProviderServerImpl : ServerImpl
	{
		private static ILog _log= LogManager.GetLogger("Lightstreamer.DotNet.Server.MetadataProviderServer");

		private bool _initExpected;
		private IMetadataProvider _adapter;
		private IDictionary _adapterParams;
		private string _adapterConfig;

        private enum ConcurrencyPolicies {
            None,
            SystemPool,
            VirtuallyUnlimited,
            Unlimited
        }

        private ConcurrencyPolicies _concurrencyPolicy;

		public MetadataProviderServerImpl() {
			_initExpected = true;
			_adapter= null;
			_adapterParams= new Hashtable();
			_adapterConfig= null;

            System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
            string policy = appSettings["Lightstreamer.Metadata.Concurrency"];
            if (policy != null) {
                try {
                    _concurrencyPolicy = (ConcurrencyPolicies)Enum.Parse(typeof(ConcurrencyPolicies), policy);
                } catch (Exception) {
                    throw new Exception("Invalid Lightstreamer.Metadata.Concurrency configuration: " + policy);
                }
            } else {
                _concurrencyPolicy = ConcurrencyPolicies.SystemPool;
            }
        }

		public IMetadataProvider Adapter {
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
		
		protected override string getSupportedVersion(string proxyVersion) {
			Debug.Assert(_maxVersion.Equals("1.9.1")); // to be kept aligned when upgrading

			if (proxyVersion != null && proxyVersion.Equals("1.9.0")) {
				// the protocols are compatible, but this identifies an old Server version
				// which doesn't support single connection for Data Adapters;
				// since this does not affect Metadata Adapters, we can accept
				_log.Info("Received Proxy Adapter protocol version as " + proxyVersion + " for Metadata Adapter " + Name + ": compatible.");
				return _maxVersion;
			}
			return base.getSupportedVersion(proxyVersion);
		}

		public override void Start() {
            _log.Info("Managing Metadata Adapter " + Name + " with concurrency policy: " + _concurrencyPolicy.ToString());

			Init(false);
			StartOut();

			IDictionary credentials = getCredentialParams(true);
			if (credentials != null) {
				SendRemoteCredentials(credentials);
			}
			StartIn();
		}

		protected void SendRemoteCredentials(IDictionary credentials) {
            String message = MetadataProviderProtocol.WriteRemoteCredentials(credentials);
            RequestManager currRequestManager;
            lock (this) {
                currRequestManager = _requestManager;
            }
            if (currRequestManager != null) {
                currRequestManager.SendUnsolicitedMessage(MetadataProviderProtocol.AUTH_REQUEST_ID, message, _log);
            }
        }

        public override void OnRequestReceived(string requestId, string request) {
			int sep= request.IndexOf(RemotingProtocol.SEP);
			if (sep < 1) {
				_log.Warn("Discarding malformed request: " + request);
				return;
			}
			
			string method= request.Substring(0, sep);

			try {
				if (method.Equals(MetadataProviderProtocol.METHOD_CLOSE)) {
					// this can also precede the init request
					if (! requestId.Equals(MetadataProviderProtocol.CLOSE_REQUEST_ID)) {
						throw new RemotingException("Unexpected id found while parsing a " + MetadataProviderProtocol.METHOD_CLOSE + " request");
					}
					IDictionary closeParams = MetadataProviderProtocol.ReadClose(request.Substring(sep + 1));
					String closeReason = (string)closeParams[MetadataProviderProtocol.KEY_CLOSE_REASON];
					Dispose();
					if (closeReason != null) {
						throw new RemotingException("Close requested by the counterpart with reason: " + closeReason);
					} else {
						throw new RemotingException("Close requested by the counterpart");
					}
				}

				bool isInitRequest = method.Equals(MetadataProviderProtocol.METHOD_METADATA_INIT);
				if (isInitRequest && ! _initExpected) {
					throw new RemotingException("Unexpected late " + MetadataProviderProtocol.METHOD_METADATA_INIT + " request");
				} else if (! isInitRequest && _initExpected) {
					throw new RemotingException("Unexpected request " + request + " while waiting for a " + MetadataProviderProtocol.METHOD_METADATA_INIT + " request");
				}

                if (isInitRequest) {
                    _log.Debug("Processing request: " + requestId);
                    _initExpected = false;
                    string keepaliveHint = null;
                    string reply;
                    IDictionary initParams = MetadataProviderProtocol.ReadInit(request.Substring(sep + 1));

                    try {
                        string proxyVersion = (string)initParams[PROTOCOL_VERSION_PARAM];
                        string advertisedVersion = getSupportedVersion(proxyVersion);
						// this may prevent the initialization

						// we can support multiple versions based on the request of the counterparty
						// and the version for this connection is indicated by advertisedVersion, 
						// but currently we only support the latest version
						Debug.Assert(advertisedVersion.Equals(_maxVersion));

                        keepaliveHint = (string)initParams[KEEPALIVE_HINT_PARAM];
                        if (keepaliveHint == null) {
                            keepaliveHint = "0";
                        }
                        initParams.Remove(PROTOCOL_VERSION_PARAM);
                        initParams.Remove(KEEPALIVE_HINT_PARAM);
                            // the version and keepalive hint are internal parameters, not to be sent to the custom Adapter

                        IEnumerator paramIter = _adapterParams.Keys.GetEnumerator();
                        while (paramIter.MoveNext()) {
                            string param = (string)paramIter.Current;
                            initParams.Add(param, _adapterParams[param]);
                        }
                        _adapter.Init(initParams, _adapterConfig);

                        IDictionary _proxyParams = new Hashtable();
                        _proxyParams.Add(PROTOCOL_VERSION_PARAM, advertisedVersion);
                        reply = MetadataProviderProtocol.WriteInit(_proxyParams);

                    } catch (Exception e) {
                        reply = MetadataProviderProtocol.WriteInit(e);
                    }
                    UseKeepaliveHint(keepaliveHint);
                    sendReply(requestId, reply);

				} else if (method.Equals(MetadataProviderProtocol.METHOD_GET_ITEM_DATA)) {
					string [] items= MetadataProviderProtocol.ReadGetItemData(request.Substring(sep +1));
                    executeAndReply(requestId, true, delegate() {
                        try {
						ItemData [] itemDatas= new ItemData [items.Length];
							for (int i= 0; i < items.Length; i++) {
								IList modeList= new ArrayList();
								if (_adapter.ModeMayBeAllowed(items[i], Mode.RAW)) modeList.Add(Mode.RAW);
								if (_adapter.ModeMayBeAllowed(items[i], Mode.MERGE)) modeList.Add(Mode.MERGE);
								if (_adapter.ModeMayBeAllowed(items[i], Mode.DISTINCT)) modeList.Add(Mode.DISTINCT);
								if (_adapter.ModeMayBeAllowed(items[i], Mode.COMMAND)) modeList.Add(Mode.COMMAND);
								Mode [] modes= new Mode [modeList.Count];
								for (int j= 0; j < modeList.Count; j++) modes[j]= (Mode) modeList[j];
								itemDatas[i]= new ItemData();
								itemDatas[i].AllowedModes= modes;
								itemDatas[i].DistinctSnapshotLength= _adapter.GetDistinctSnapshotLength(items[i]);
								itemDatas[i].MinSourceFrequency= _adapter.GetMinSourceFrequency(items[i]);
                            }
                            return MetadataProviderProtocol.WriteGetItemData(itemDatas);
                        } catch (Exception e) {
                            return MetadataProviderProtocol.WriteGetItemData(e);
                        }
                    });
				
				} else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_USER)) {
                    NotifyUserData notifyUserData = MetadataProviderProtocol.ReadNotifyUser(request.Substring(sep + 1),
                                                                        MetadataProviderProtocol.METHOD_NOTIFY_USER);
                    executeAndReply(requestId, false, delegate() {
                        try {
                            _adapter.NotifyUser(notifyUserData.User, notifyUserData.Password, notifyUserData.httpHeaders);
							UserData userData= new UserData();
							userData.AllowedMaxBandwidth= _adapter.GetAllowedMaxBandwidth(notifyUserData.User);
							userData.WantsTablesNotification= _adapter.WantsTablesNotification(notifyUserData.User);
                            return MetadataProviderProtocol.WriteNotifyUser(userData, MetadataProviderProtocol.METHOD_NOTIFY_USER);
                        } catch (Exception e) {
                            return MetadataProviderProtocol.WriteNotifyUser(e, MetadataProviderProtocol.METHOD_NOTIFY_USER);
                        }
                    });

                }
                else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_USER_AUTH))
                {
                    NotifyUserData notifyUserData = MetadataProviderProtocol.ReadNotifyUser(request.Substring(sep + 1),
                                                                        MetadataProviderProtocol.METHOD_NOTIFY_USER_AUTH);
                    executeAndReply(requestId, false, delegate() {
                        try {
                            _adapter.NotifyUser(notifyUserData.User, notifyUserData.Password, notifyUserData.httpHeaders, notifyUserData.clientPrincipal);
                            UserData userData = new UserData();
                            userData.AllowedMaxBandwidth = _adapter.GetAllowedMaxBandwidth(notifyUserData.User);
                            userData.WantsTablesNotification = _adapter.WantsTablesNotification(notifyUserData.User);
                            return MetadataProviderProtocol.WriteNotifyUser(userData, MetadataProviderProtocol.METHOD_NOTIFY_USER_AUTH);
                        } catch (Exception e) {
                            return MetadataProviderProtocol.WriteNotifyUser(e, MetadataProviderProtocol.METHOD_NOTIFY_USER_AUTH);
                        }
                    });

                }
                else if (method.Equals(MetadataProviderProtocol.METHOD_GET_SCHEMA))
                {
					GetSchemaData getSchemaData= MetadataProviderProtocol.ReadGetSchema(request.Substring(sep +1));
                    executeAndReply(requestId, true, delegate() {
                        try {
							string [] fields= _adapter.GetSchema(getSchemaData.User, getSchemaData.Session, getSchemaData.Group, getSchemaData.Schema);
						    if (fields == null) {
							    fields = new string [0];
						    }
						    if (fields.Length == 0) {
							    _log.Warn("Null or empty field list from getSchema for schema '" + getSchemaData.Schema + "' in group '" + getSchemaData.Group + "'");
						    }
						    return MetadataProviderProtocol.WriteGetSchema(fields);
					    } catch (Exception e) {
						    return MetadataProviderProtocol.WriteGetSchema(e);
					    }
                    });
				
				} else if (method.Equals(MetadataProviderProtocol.METHOD_GET_ITEMS)) {
					GetItemsData getItemsData= MetadataProviderProtocol.ReadGetItems(request.Substring(sep +1));
                    executeAndReply(requestId, true, delegate() {
                        try {
							string [] items= _adapter.GetItems(getItemsData.User, getItemsData.Session, getItemsData.Group);
						    if (items == null) {
							    items = new string [0];
						    }
						    if (items.Length == 0) {
							    _log.Warn("Null or empty item list from getItems for group '" + getItemsData.Group + "'");
						    }
						    return MetadataProviderProtocol.WriteGetItems(items);
					    } catch (Exception e) {
						    return MetadataProviderProtocol.WriteGetItems(e);
					    }
                    });
				
				} else if (method.Equals(MetadataProviderProtocol.METHOD_GET_USER_ITEM_DATA)) {
					GetUserItemData getUserItemData= MetadataProviderProtocol.ReadGetUserItemData(request.Substring(sep +1));
                    executeAndReply(requestId, true, delegate() {
                        try {
							UserItemData [] userItemDatas= new UserItemData[getUserItemData.Items.Length];
						    for (int i= 0; i < getUserItemData.Items.Length; i++) {
							    IList modeList= new ArrayList();
							    if (_adapter.IsModeAllowed(getUserItemData.User, getUserItemData.Items[i], Mode.RAW)) modeList.Add(Mode.RAW);
							    if (_adapter.IsModeAllowed(getUserItemData.User, getUserItemData.Items[i], Mode.MERGE)) modeList.Add(Mode.MERGE);
							    if (_adapter.IsModeAllowed(getUserItemData.User, getUserItemData.Items[i], Mode.DISTINCT)) modeList.Add(Mode.DISTINCT);
							    if (_adapter.IsModeAllowed(getUserItemData.User, getUserItemData.Items[i], Mode.COMMAND)) modeList.Add(Mode.COMMAND);
							    Mode [] modes= new Mode [modeList.Count];
							    for (int j= 0; j < modeList.Count; j++) modes[j]= (Mode) modeList[j];
							    userItemDatas[i]= new UserItemData();
							    userItemDatas[i].AllowedModes= modes;
							    userItemDatas[i].AllowedMaxItemFrequency= _adapter.GetAllowedMaxItemFrequency(getUserItemData.User, getUserItemData.Items[i]);
							    userItemDatas[i].AllowedBufferSize= _adapter.GetAllowedBufferSize(getUserItemData.User, getUserItemData.Items[i]);
						    }
						    return MetadataProviderProtocol.WriteGetUserItemData(userItemDatas);
					    } catch (Exception e) {
						    return MetadataProviderProtocol.WriteGetUserItemData(e);
					    }
                    });
				
				} else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_USER_MESSAGE)) {
					NotifyUserMessageData notifyUserMessageData= MetadataProviderProtocol.ReadNotifyUserMessage(request.Substring(sep +1));
                    executeAndReply(requestId, false, delegate() {
                        try {
                            _adapter.NotifyUserMessage(notifyUserMessageData.User, notifyUserMessageData.Session, notifyUserMessageData.Message);
						    return MetadataProviderProtocol.WriteNotifyUserMessage();
					    } catch (Exception e) {
						    return MetadataProviderProtocol.WriteNotifyUserMessage(e);
					    }
                    });
				
				} else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_NEW_SESSION)) {
					NotifyNewSessionData notifyNewSessionData= MetadataProviderProtocol.ReadNotifyNewSession(request.Substring(sep +1));
                    executeAndReply(requestId, false, delegate() {
                        try {
                            _adapter.NotifyNewSession(notifyNewSessionData.User, notifyNewSessionData.Session, notifyNewSessionData.clientContext);
						    return MetadataProviderProtocol.WriteNotifyNewSession();
					    } catch (Exception e) {
						    return MetadataProviderProtocol.WriteNotifyNewSession(e);
					    }
                    });
				
				} else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_SESSION_CLOSE)) {
					string session= MetadataProviderProtocol.ReadNotifySessionClose(request.Substring(sep +1));
                    executeAndReply(requestId, true, delegate() {
                        try {
                            _adapter.NotifySessionClose(session);
						    return MetadataProviderProtocol.WriteNotifySessionClose();
					    } catch (Exception e) {
						    return MetadataProviderProtocol.WriteNotifySessionClose(e);
					    }
                    });

				} else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_NEW_TABLES)) {
					NotifyNewTablesData notifyNewTablesData= MetadataProviderProtocol.ReadNotifyNewTables(request.Substring(sep +1));
                    executeAndReply(requestId, false, delegate() {
                        try {
                            _adapter.NotifyNewTables(notifyNewTablesData.User, notifyNewTablesData.Session, notifyNewTablesData.Tables);
						    return MetadataProviderProtocol.WriteNotifyNewTables();
					    } catch (Exception e) {
						    return MetadataProviderProtocol.WriteNotifyNewTables(e);
					    }
                    });
				
				} else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_TABLES_CLOSE)) {
					NotifyTablesCloseData notifyTablesCloseData= MetadataProviderProtocol.ReadNotifyTablesClose(request.Substring(sep +1));
                    executeAndReply(requestId, true, delegate() {
                        try {
                            _adapter.NotifyTablesClose(notifyTablesCloseData.Session, notifyTablesCloseData.Tables);
						    return MetadataProviderProtocol.WriteNotifyTablesClose();
					    } catch (Exception e) {
						    return MetadataProviderProtocol.WriteNotifyTablesClose(e);
					    }
                    });

                } else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_MPN_DEVICE_ACCESS)) {
                    NotifyMpnDeviceAccessData notifyMpnDeviceAccessData = MetadataProviderProtocol.ReadNotifyMpnDeviceAccess(request.Substring(sep + 1));
                    executeAndReply(requestId, false, delegate() {
                        try {
                            _adapter.NotifyMpnDeviceAccess(notifyMpnDeviceAccessData.User, notifyMpnDeviceAccessData.SessionID, notifyMpnDeviceAccessData.Device);
                            return MetadataProviderProtocol.WriteNotifyMpnDeviceAccess();
                        } catch (Exception e) {
                            return MetadataProviderProtocol.WriteNotifyMpnDeviceAccess(e);
                        }
                    });

                } else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION)) {
                    NotifyMpnSubscriptionActivationData notifyMpnSubscriptionActivationData = MetadataProviderProtocol.ReadNotifyMpnSubscriptionActivation(request.Substring(sep + 1));
                    executeAndReply(requestId, false, delegate() {
                        try {
                            _adapter.NotifyMpnSubscriptionActivation(notifyMpnSubscriptionActivationData.User, notifyMpnSubscriptionActivationData.SessionID, notifyMpnSubscriptionActivationData.Table, notifyMpnSubscriptionActivationData.MpnSubscription);
                            return MetadataProviderProtocol.WriteNotifyMpnSubscriptionActivation();
                        } catch (Exception e) {
                            return MetadataProviderProtocol.WriteNotifyMpnSubscriptionActivation(e);
                        }
                    });

                } else if (method.Equals(MetadataProviderProtocol.METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE)) {
                    NotifyMpnDeviceTokenChangeData notifyMpnDeviceTokenChangeData = MetadataProviderProtocol.ReadNotifyMpnDeviceTokenChange(request.Substring(sep + 1));
                    executeAndReply(requestId, false, delegate() {
                        try {
                            _adapter.NotifyMpnDeviceTokenChange(notifyMpnDeviceTokenChangeData.User, notifyMpnDeviceTokenChangeData.SessionID, notifyMpnDeviceTokenChangeData.Device, notifyMpnDeviceTokenChangeData.NewDeviceToken);
                            return MetadataProviderProtocol.WriteNotifyMpnDeviceTokenChange();
                        } catch (Exception e) {
                            return MetadataProviderProtocol.WriteNotifyMpnDeviceTokenChange(e);
                        }
                    });

                } else {
                    _log.Warn("Discarding unknown request: " + request);
                }
				
			} catch (Exception e) {
				OnException(e);
			}
		}

        private delegate String Worker();
        private delegate void Process();

        private void executeAndReply(string requestId, bool isSimple, Worker fun) {
            Process proc = delegate() {
                _log.Debug("Processing request: " + requestId);
                string reply = fun();
                sendReply(requestId, reply);
            };
            ConcurrencyPolicies policy = _concurrencyPolicy;
            if (policy == ConcurrencyPolicies.VirtuallyUnlimited) {
                if (isSimple) {
                    policy = ConcurrencyPolicies.SystemPool;
                } else {
                    policy = ConcurrencyPolicies.Unlimited;
                }
            }
            if (policy == ConcurrencyPolicies.None) {
                proc();
            } else if (policy == ConcurrencyPolicies.SystemPool) {
                TaskCreationOptions blocking = TaskCreationOptions.PreferFairness;
                /*
                if (! isSimple) { 
                    blocking |= TaskCreationOptions.LongRunning;
                }
                We could do this to take potentially blocking tasks into account,
                but we prefer to leverage the thread pool limit also in this case;
                to enforce separate threads for the potentially blocking tasks
                you can use the VirtuallyUnlimited setting
                */
                Task.Factory.StartNew(() => proc(), blocking);
            } else { // ConcurrencyPolicies.Unlimited
                Thread thread = new Thread(new ThreadStart(proc));
                thread.Start();
            }
        }
        
        private void sendReply(string requestId, string reply) {
            try {
                RequestManager currRequestManager;
                lock (this) {
                    currRequestManager = _requestManager;
                }
                if (currRequestManager != null) {
                    currRequestManager.SendReply(requestId, reply, _log);
                }
            } catch (Exception e) {
                OnException(e);
            }
        }

		public override bool handleIOException(Exception exception) {
			_log.Error("Caught exception: " + exception.Message, exception);
			Exception cause= exception.GetBaseException();
			_log.Fatal("Exception caught while reading/writing from/to streams: '" + cause.Message + "', aborting...", cause);

			System.Environment.Exit(0);
			return false;
		}

		public override bool handleException(Exception exception) {
			_log.Error("Caught exception: " + exception.Message, exception);
			return false;
		}
	}

}
