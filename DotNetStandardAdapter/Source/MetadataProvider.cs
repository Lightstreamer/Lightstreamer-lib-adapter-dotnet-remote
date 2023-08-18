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

namespace Lightstreamer.Interfaces.Metadata {

    // ////////////////////////////////////////////////////////////////////////
    // Exceptions

    /// <summary>
    /// Thrown by the Init method in <see cref="IMetadataProvider"/> if there is some problem that prevents the correct
    /// behavior of the Metadata Adapter. If this exception occurs, Lightstreamer Kernel must give up the startup.
    /// </summary>
    public class MetadataProviderException : MetadataException {

		/// <summary>
		/// Constructs a MetadataProviderException with a supplied error message text.
		/// </summary>
		/// <param name="msg">The detail message.</param>
		public MetadataProviderException(string msg) : base(msg) {}
	}

    /// <summary>
    /// Thrown by the Notify* methods in <see cref="IMetadataProvider"/> if the supplied User is not recognized or
    /// a functionality is not implemented for this User.
    /// </summary>
    public class AccessException : MetadataException {

		/// <summary>
		/// Constructs an AccessException with a supplied error message text.
		/// </summary>
		/// <param name="msg">The detail message.</param>
		public AccessException(string msg) : base(msg) {}
	}

    /// <summary>
    /// Thrown by the Notify* methods in <see cref="IMetadataProvider"/> if some functionality cannot be allowed
    /// to the supplied User. This may occur if the user is not granted some resource or if the user
    /// would exceed the granted amount. Different kinds of problems can be distinguished by an error code.
    /// Both the error message detail and the error code will be forwarded by Lightstreamer Kernel to the Client.
    /// </summary>
    public class CreditsException : MetadataException {
		private int _clientErrorCode;
		private string _clientErrorMsg;

		/// <summary>
		/// Constructs a CreditsException with supplied error code and message text.
		/// </summary>
		/// <param name="clientErrorCode">Error code that can be used to distinguish the kind of problem. It must
		/// be a negative integer, or zero to mean an unspecified problem.</param>
		/// <param name="msg">The detail message.</param>
		public CreditsException(int clientErrorCode, string msg) : base(msg) {
			_clientErrorCode= clientErrorCode;
		}

		/// <summary>
		/// Constructs a CreditsException with supplied error code and message text to be forwarded to the Client.
		/// An internal error message text can also be specified.
		/// </summary>
		/// <param name="clientErrorCode">Error code that can be used to distinguish the kind of problem. It must
		/// be a negative integer, or zero to mean an unspecified problem.</param>
		/// <param name="msg">The detail message.</param>
		/// <param name="userMsg">A detail message to be forwarded to the Client.
		/// It can be null, in which case an empty string message will be forwarded.
		/// The message is free, but if it is not in simple ASCII or if it is
		/// multiline, it might be altered in order to be sent to very old
		/// non-TLCP clients.</param>
		public CreditsException(int clientErrorCode, string msg, string userMsg) : base(msg) {
			_clientErrorCode= clientErrorCode;
			_clientErrorMsg= userMsg;
		}

		/// <value>
		/// Readonly. The error code to be forwarded to the client.
		/// </value>
		public int ClientErrorCode {
			get {
				return _clientErrorCode;
			}
		}

		/// <value>
		/// Readonly. The error detail message to be forwarded to the client.
		/// If the message is not in simple ASCII or is in multiline format,
		/// the real text sent to very old non-TLCP clients might be altered.
		/// If null, an empty string message will be forwarded instead.
		/// </value>
		public string ClientErrorMsg {
			get {
				return _clientErrorMsg;
			}
		}
	}

    /// <summary>
    /// <para>Thrown by the NotifyNewSession method of <see cref="IMetadataProvider"/>
    /// if a User is not enabled to open a new Session but he would be enabled
    /// as soon as another Session were closed. By using this exception,
    /// the ID of the other Session is also supplied.</para>
    /// <para>After receiving this exception, the Server may try to close
    /// the specified session and invoke NotifyNewSession again.</para>
    /// </summary>
    public class ConflictingSessionException : CreditsException {
		private string _conflictingSessionID;

		/// <summary>
		/// Constructs a ConflictingSessionException with supplied error code and message text
        /// that will be forwarded to the Client in case the Server can't solve the issue
        /// by closing the conflicting session. An internal error message text can also be specified.
		/// </summary>
		/// <param name="clientErrorCode">Error code that can be used to distinguish the kind of problem. It must
		/// be a negative integer, or zero to mean an unspecified problem.</param>
		/// <param name="msg">The detail message.</param>
		/// <param name="userMsg">A detail message to be forwarded to the Client.
		/// It can be null, in which case an empty string message will be forwarded.
		/// The message is free, but if it is not in simple ASCII or if it is
		/// multiline, it might be altered in order to be sent to very old
		/// non-TLCP clients.</param>
		/// <param name="conflictingSessionID">ID of a Session that can be closed in order to eliminate the
		/// reported problem. It must not be null.</param>
		public ConflictingSessionException(int clientErrorCode, string msg, string userMsg, string conflictingSessionID) : base(clientErrorCode, msg, userMsg) {
			_conflictingSessionID= conflictingSessionID;
		}

		/// <value>
        /// Readonly. The ID of a Session that can be closed in order
        /// to eliminate the problem reported in this exception.
		/// </value>
		public string ConflictingSessionID {
			get {
				return _conflictingSessionID;
			}
		}
	}

    /// <summary>
    /// Thrown by the GetItems and GetSchema methods in <see cref="IMetadataProvider"/> if the supplied
    /// Item Group name (or Item List specification) is not recognized or cannot be resolved.
    /// </summary>
    public class ItemsException : MetadataException {

		/// <summary>
		/// Constructs an ItemsException with a supplied error message text.
		/// </summary>
		/// <param name="msg">The detail message.</param>
		public ItemsException(string msg) : base(msg) {}
	}

    /// <summary>
    /// Thrown by the GetSchema method in <see cref="IMetadataProvider"/> if the supplied
    /// Field Schema name (or Field List specification) is not recognized or cannot be resolved.
    /// </summary>
    public class SchemaException : MetadataException {

		/// <summary>
		/// Constructs a SchemaException with a supplied error message text.
		/// </summary>
		/// <param name="msg">The detail message.</param>
		public SchemaException(string msg) : base(msg) {}
	}

    /// <summary>
    /// Thrown by the Notify* methods in <see cref="IMetadataProvider"/> if there is some inconsistency in the supplied
    /// parameters. Lightstreamer Kernel ensures that such conditions will never occur, but they may
    /// be checked for debugging or documentation reasons.
    /// </summary>
    public class NotificationException : MetadataException {

		/// <summary>
		/// Constructs a NotificationException with a supplied error message text.
		/// </summary>
		/// <param name="msg">The detail message.</param>
		public NotificationException(string msg) : base(msg) {}
	}

	// ////////////////////////////////////////////////////////////////////////
	// Support classes and constants

	/// <summary>
	/// Encapsulates a publishing Mode. The different Modes handled by Lightstreamer Kernel can be uniquely
	/// identified by the static constants defined in this class. See the technical documents for a detailed
	/// description of Modes.
	/// </summary>
	public class Mode {

        /// <value>The RAW Mode.</value>
        public static readonly Mode RAW= new Mode("RAW");

        /// <value>The MERGE Mode.</value>
        public static readonly Mode MERGE= new Mode("MERGE");

        /// <value>The DISTINCT Mode.</value>
        public static readonly Mode DISTINCT= new Mode("DISTINCT");

        /// <value>The COMMAND Mode.</value>
        public static readonly Mode COMMAND= new Mode("COMMAND");

		private string _name;

		private Mode(string name) {
			_name= name;
		}

		/// <summary>Method ToString.</summary>
		/// <returns> ...
		/// </returns>
		public override string ToString()
		{
			return _name;
		}

		/// <summary>Method Equals.</summary>
        /// <param name="other"> ...
        /// </param>
        /// <returns> ...
		/// </returns>
		public override bool Equals(object other)
		{
			Mode mode= other as Mode;
			if (mode == null) return false;

			return mode._name.Equals(_name);
		}

		/// <summary>Method GetHashCode.</summary>
		/// <returns>...
		/// </returns>
		public override int GetHashCode()
		{
			return _name.GetHashCode();
		}
	}

    /// <summary>
    /// Used by IMetadataProvider to provide value objects to the calls
    /// to methods NotifyNewTables, NotifyTablesClose, and NotifyMpnSubscriptionActivation.
    /// The attributes of every Table (i.e.: Subscription) to be added or removed
    /// to a Session have to be written to a TableInfo instance.
    /// </summary>
    public class TableInfo {
		private int _winIndex;
		private Mode _mode;
		private string _group;
        private string _dataAdapter;
		private string _schema;
		private int _min;
		private int _max;
		private string _selector;
        private string [] _itemNames;

        /// <summary>
        /// Used by Lightstreamer to creates a TableInfo instance,
        /// collecting the various attributes of a Table (i.e.: Subscription).
        /// </summary>
        /// <param name="winIndex">Unique identifier of the client subscription request
        /// within the session.</param>
        /// <param name="mode">Publishing Mode for the Items in the Table (i.e. Subscription)
        /// (it must be the same across all the Table).</param>
        /// <param name="group">The name of the Item Group (or specification of the Item List)
        /// to which the subscribed Items belong.</param>
        /// <param name="dataAdapter">The name of the Data Adapter to which the Table
        /// (i.e. Subscription) refers.</param>
        /// <param name="schema">The name of the Field Schema (or specification of the Field List)
        /// used for the subscribed Items.</param>
        /// <param name="min">The 1-based index of the first Item in the Group to be considered in the
        /// Table (i.e. Subscription).</param>
        /// <param name="max">The 1-based index of the last Item in the Group to be considered in the
        /// Table (i.e. Subscription).</param>
        /// <param name="selector">The name of the optional Selector associated to the table (i.e. Subscription).</param>
        /// <param name="itemNames">The array of Item names involved in this Table (i.e. Subscription).</param>
        public TableInfo(int winIndex, Mode mode, string group, string dataAdapter, string schema, int min, int max, string selector, string [] itemNames) {
			_winIndex= winIndex;
			_mode= mode;
			_group= group;
            _dataAdapter= dataAdapter;
			_schema= schema;
			_min= min;
			_max= max;
			_selector= selector;
            _itemNames= itemNames;
		}

		/// <value>
        /// Readonly. Unique identifier of the client subscription request within the session.
        /// This allows for matching the corresponding subscription and unsubscription requests.
        /// Note that, for clients based on a very old version of a client library
        /// or text protocol, subscription requests may involve multiple Tables
        /// (i.e.: Subscriptions), hence multiple objects of this type can be supplied
        /// in a single array by IMetadataProvider through NotifyNewTables and
        /// NotifyTablesClose. In this case, the value returned
        /// is the same for all these objects and the single Tables (i.e.: Subscriptions)
        /// can be identified by their relative position in the array.
		/// </value>
		public int WinIndex {
			get {
				return _winIndex;
			}
		}

        /// <value>
        /// Readonly. The publishing Mode for the Items in the Table (i.e. Subscription)
        /// (it must be the same across all the Table).
        /// </value>
        public Mode Mode {
			get {
				return _mode;
			}
		}

        /// <value>
        /// Readonly. The name of the Item Group (or specification of the Item List)
        /// to which the subscribed Items belong.
        /// </value>
        public string Id {
			get {
				return _group;
			}
		}

        /// <value>
        /// Readonly. The name of the Data Adapter to which the Table (i.e. Subscription) refers.
        /// </value>
        public string DataAdapter
        {
            get
            {
                return _dataAdapter;
            }
        }

        /// <value>
        /// Readonly. The name of the Field Schema (or specification of the Field List)
        /// used for the subscribed Items.
        /// </value>
        public string Schema {
			get {
				return _schema;
			}
		}

        /// <value>
        /// Readonly. The index of the first Item in the Group
        /// to be considered in the Table (i.e. Subscription).
        /// </value>
        public int Min {
			get {
				return _min;
			}
		}

        /// <value>
        /// Readonly. The index of the last Item in the Group
        /// to be considered in the Table (i.e. Subscription).
        /// </value>
        public int Max {
			get {
				return _max;
			}
		}

        /// <value>
        /// Readonly. The name of the optional Selector associated to the Table (i.e. Subscription).
        /// </value>
        public string Selector {
			get {
				return _selector;
			}
        }

        /// <value>
        /// Readonly. The array of the Item names involved in this Table (i.e. Subscription).
        /// The sequence of names is the same one returned by GetItems in <see cref="IMetadataProvider"/> 
        /// when decoding of the group name, but restricted, in case a first and/or last
        /// Item was specified in the client request(see <see cref="Min"/> and <see cref="Max"/>). 
        /// </value>
        public string [] SubscribedItems
        {
            get
            {
                return _itemNames;
            }
        }
    }

    /// <summary>
    /// <para>Identifies a Push Notifications platform type, used with MPN-related requests of the MetadataProvider.</para>
    /// <para>It is used by Lightstreamer to specify the platform associated with the notified client requests.</para>
    /// <para>The available constants refer to the platform types currently supported.</para>
    /// <br/>
    /// <para><B>Edition Note:</B> Push Notifications is an optional feature,
    /// available depending on Edition and License Type.
	/// To know what features are enabled by your license, please see the License tab of the
	/// Monitoring Dashboard (by default, available at /dashboard).</para>
    /// </summary>
    public class MpnPlatformType {
        private string _name;

        /// <summary>
        /// Used by Lightstreamer to create a MpnPlatformType instance.
        /// </summary>
        /// <param name="name">A platform type name</param>
        public MpnPlatformType(string name) {
            _name= name;
        }

        /// <value>
        /// Readonly. The internal name of the platform type.
        /// </value>
        public string Name {
            get {
                return _name;
            }
        }

        /// <value>
        /// <para>Readonly. Refers to Push Notifications for Apple platforms, such as iOS, macOS and tvOS.
        /// The back-end service for Apple platforms is APNs ("Apple Push Notification service").</para>
        /// <para>Apple, iOS, macOS and tvOS are registered trademarks of Apple, Inc.</para>
        /// </value>
        public static readonly MpnPlatformType Apple= new MpnPlatformType("Apple");

        /// <value>
        /// <para>Readonly. Refers to Push Notifications for Google platforms, such as Android and Chrome.
        /// The back-end service for Google platforms is FCM ("Firebase Cloud Messaging").</para>
        /// <para>Google, Android and Chrome are registered trademarks of Google Inc.</para>
        /// </value>
        public static readonly MpnPlatformType Google= new MpnPlatformType("Google");

        /// <summary>
        /// <para>Returns a string representation of the MpnPlatformType.
        /// An MpnPlatformType object is represented by its internal name.
        /// E.g.:</para>
        /// <para>Apple</para>
        /// </summary>
        /// <returns>a string representation of the MpnPlatformType</returns>
        public override string ToString() {
            return _name;
        }

        /// <summary>
        /// Indicates whether some other object is "equal to" this one.
        /// Two MpnPlatformType objects are equal if their internal names are equal.
        /// </summary>
        /// <param name="other">The other object to be compared.</param>
        /// <returns>true if the two objects are equal.</returns>
        public override bool Equals(object other) {
            MpnPlatformType platformType = other as MpnPlatformType;
            if (platformType == null) return false;

            return platformType._name.Equals(_name);
        }

        /// <summary>
        /// Returns a hash code value for the object.
        /// </summary>
        /// <returns>Hash code value for the object.</returns>
        public override int GetHashCode() {
            return _name.GetHashCode();
        }
    }

    /// <summary>
    /// <para>
    /// Specifies a target device for Push Notifications, used with MPN-related requests for the MetadataProvider.
    /// Note that the processing and the authorization of Push Notifications is per-device and per-application.
    /// While a physical device is uniquely identified by the platform type and a platform dependent device token,
    /// Lightstreamer considers the same device used by two different applications as two different MPN devices.
    /// Thus, an MpnDeviceInfo instance uniquely identifies both the physical device and the application for which
    /// it is being used.
    /// </para>
    /// <para>An MpnDeviceInfo always provides the following identifiers:</para>
    /// <para>- The platform type.</para>
    /// <para>- The application ID.</para>
    /// <para>- The device token.</para>
    /// <br/>
    /// <para><B>Edition Note:</B> Push Notifications is an optional feature,
    /// available depending on Edition and License Type.
	/// To know what features are enabled by your license, please see the License tab of the
	/// Monitoring Dashboard (by default, available at /dashboard).</para>
    /// </summary>
    public class MpnDeviceInfo {
        private MpnPlatformType _type;
        private string _applicationId;
        private string _deviceToken;

        /// <summary>
        /// Used by Lightstreamer to provide a MpnDeviceInfo instance to the MPN-related methods.
        /// </summary>
        /// <param name="type">Platform type of the device.</param>
        /// <param name="applicationId">The app ID, also known as the bundle ID on some platforms.</param>
        /// <param name="deviceToken">The token of the device.</param>
        public MpnDeviceInfo(
                MpnPlatformType type,
                string applicationId,
                string deviceToken) {

            _type = type;
            _applicationId = applicationId;
            _deviceToken = deviceToken;
        }

        /// <value>
        /// Readonly. Platform type of the device.
        /// </value>
        public MpnPlatformType Type {
            get {
                return _type;
            }
        }

        /// <value>
        /// Readonly. Application ID, also known as the package name or bundle ID on some platforms.
        /// </value>
        public string ApplicationId {
            get {
                return _applicationId;
            }
        }

        /// <value>
        /// Readonly. Token of the device, also know as the registration ID on some platforms.
        /// </value>
        public string DeviceToken {
            get {
                return _deviceToken;
            }
        }

        /// <summary>
        /// An MpnDeviceInfo object is represented by a juxtaposition of its three properties
        /// platform type, application ID and device token, separated by a single "/" character.
        /// E.g.:
        /// <para>Apple/com.lightstreamer.ios.stocklist/8fac[...]fe12</para>
        /// </summary>
        /// <returns>Returns a string representation of the MpnDeviceInfo.</returns>
        public override string ToString()
        {
            return _type.Name + "/" + _applicationId + "/" + _deviceToken;
        }

        /// <summary>
        /// Indicates whether some other object is "equal to" this one.
        /// Two MpnDeviceInfo objects are equal if their three properties are equal.
        /// </summary>
        /// <param name="other">The other object to be compared.</param>
        /// <returns>true if the two objects are equal.</returns>
        public override bool Equals(object other)
        {
            MpnDeviceInfo deviceInfo = other as MpnDeviceInfo;
            return _type.Equals(deviceInfo._type) &&
                    _applicationId.Equals(deviceInfo._applicationId) &&
                    _deviceToken.Equals(deviceInfo._deviceToken);
        }

        /// <summary>
        /// Returns a hash code value for the object.
        /// </summary>
        /// <returns>Hash code value for the object.</returns>
        public override int GetHashCode()
        {
            return _type.GetHashCode() ^ _applicationId.GetHashCode() ^ _deviceToken.GetHashCode();
        }
    }

    /// <summary>
    /// <para>Specifies a Push Notifications subscription, used with MPN-related requests of the MetadataProvider.</para>
    /// <para>For the actual description of the subscription we rely on a generic descriptor accessible
    /// via the NotificationFormat property, where the structure of the descriptor depends on the platform.</para>
    /// <br/>
    /// <para><B>Edition Note:</B> Push Notifications is an optional feature,
    /// available depending on Edition and License Type.
	/// To know what features are enabled by your license, please see the License tab of the
	/// Monitoring Dashboard (by default, available at /dashboard).</para>
    /// </summary>
    public class MpnSubscriptionInfo {
        private MpnDeviceInfo _device;
        private string _notification;
        private string _trigger;

        /// <summary>
        /// Used by Lightstreamer to create a MpnSubscriptionInfo instance.
        /// </summary>
        /// <param name="device">The MPN device of the push notifications.</param>
        /// <param name="notification">The descriptor of the push notifications format.</param>
        /// <param name="trigger">The expression the updates are checked against to trigger the notification.</param>
        public MpnSubscriptionInfo(
                MpnDeviceInfo device,
                string notification,
                string trigger) {

            _device= device;
            _notification = notification;
            _trigger = trigger;
        }

        /// <value>
        /// Readonly. MPN device of this subscription.
        /// </value>
        public MpnDeviceInfo Device
        {
            get
            {
                return _device;
            }
        }

        /// <value>
        /// Readonly. The descriptor of the push notifications format of this subscription.
        /// The structure of the format descriptor depends on the platform type
        /// and it is represented in json.
        /// </value>
        public string NotificationFormat
        {
            get
            {
                return _notification;
            }
        }

        /// <value>
        /// Readonly. Optional expression that triggers the delivery of push notification.
        /// </value>
        public String Trigger {
            get {
                return _trigger;
            }
        }

        /// <summary>
        /// An MpnSubscriptionInfo object is represented by its three properties
        /// device, trigger and notification format, prefixed by their name and
        /// on separate lines.E.g.:
        /// <para>device=Apple/com.lightstreamer.ios.stocklist/8fac[...] fe12\n</para>
        /// <para>trigger=Double.parseDouble(${last_price}) >= 50.0\n</para>
        /// <para>notificationFormat={aps={badge=AUTO, alert=Price is over 50$, sound=Default}}\n</para>
        /// </summary>
        /// <returns>Returns a string representation of the MpnSubscriptionInfo.</returns>
        public override string ToString()
        {
            return "device=" + _device + Environment.NewLine +
                    "trigger=" + _trigger + Environment.NewLine +
                    "notificationFormat=" + _notification;
        }

        /// <summary>
        /// Indicates whether some other object is "equal to" this one.
        /// Two MpnSubscriptionInfo objects are equal if their three properties are equal.
        /// </summary>
        /// <param name="other">The other object to be compared.</param>
        /// <returns>true if the two objects are equal.</returns>
        public override bool Equals(object other)
        {
            MpnSubscriptionInfo subscriptionInfo = other as MpnSubscriptionInfo;
            return _device.Equals(subscriptionInfo._device) &&
                   ((_trigger != null) ? _trigger.Equals(subscriptionInfo._trigger) : (subscriptionInfo._trigger == null)) &&
                   _notification.Equals(subscriptionInfo._notification);
        }

        /// <summary>
        /// Returns a hash code value for the object.
        /// </summary>
        /// <returns>Hash code value for the object.</returns>
        public override int GetHashCode()
        {
            return _device.GetHashCode() ^ ((_trigger != null) ? _trigger.GetHashCode() : 0) ^ _notification.GetHashCode();
        }
    }

    // ////////////////////////////////////////////////////////////////////////
    // Interfaces

    /// <summary>
    /// <para>Provides an interface to be implemented by a Remote Metadata Adapter in order
    /// to attach a Metadata Provider to Lightstreamer.
    /// A single instance of a Remote Metadata Adapter is created by Lightstreamer
    /// through the launch of a Remote Server, based on configured class name and parameters.
    /// For this purpose, any Remote Metadata Adapter must provide a void constructor.
    /// Alternatively, an instance of a Remote Metadata Adapter is supplied to Lightstreamer
    /// programmatically through a <see cref="Lightstreamer.DotNet.Server.MetadataProviderServer"/> instance.</para>
	/// <para>A Metadata Provider is used by Lightstreamer Kernel
	/// in combination with one or multiple Data Providers, uniquely associated with it; it is consulted
	/// in order to manage the push Requests intended for the associated Data Providers.
	/// A Metadata Provider supplies information for several different goals:</para>
	/// <para>- the resolution of the macro names used in the Requests;</para>
	/// <para>- the check of the User accessibility to the requested Items;</para>
	/// <para>- the check of the resource level granted to the User;</para>
	/// <para>- the request for specific characteristics of the Items.</para>
    /// <para>Note: Each Item may be supplied by one or more of the associated Data
    /// Adapters and each client Request must reference to a specific Data Adapter.
    /// However, in the current version of the interface, no Data Adapter
    /// information is supplied to the Metadata Adapter methods. Hence, the Item
    /// names must provide enough information for the methods to give an answer.
    /// As a consequence, for instance, the frequency, snapshot length and other
    /// characteristics of an Item are the same regardless of the Data Adapter
    /// it is requested from. More likely, for each Item name defined, only one
    /// of the Data Adapters in the set is responsible for supplying that Item.</para>
    /// <para>All implementation methods should perform as fast as possible.
    /// See the notes on the corresponding methods in the Java In-Process interface
    /// for the method-related details. Also consider that the roundtrip time
    /// involved in the remote call adds up to each call time anyway.</para>
    /// <para>In order to avoid that delays on calls for one session
    /// propagate to other sessions, the size of the thread pool devoted to the
    /// management of the client requests should be properly set, through the
    /// "server_pool_max_size" flag, in the Server configuration file.</para>
    /// <para>Alternatively, a dedicated pool, properly sized, can be defined
    /// for the involved Adapter Set in �adapters.xml�. Still more restricted
    /// dedicated pools can be defined for the authorization-related calls
    /// and for each Data Adapter in the Adapter Set. The latter pool would also
    /// run any Metadata Adapter method related to the items supplied by the
    /// specified Data Adapter.</para>
	/// </summary>
	public interface IMetadataProvider {

        /// <summary>
        /// <para>Called by the Remote Server to provide initialization information
        /// to the Metadata Adapter.
        /// If an exception occurs in this method, Lightstreamer Kernel can't complete the startup and must
        /// exit. The initialization information can be supplied in different ways, depending on the way the
        /// Remote Server is launched.</para>
        /// <para>The call must not be blocking; any polling cycle or similar must be
        /// started in a different thread. Any delay in returning from this call
        /// will in turn delay the Server initialization.
        /// If an exception occurs in this method, Lightstreamer Server can't
        /// complete the startup and must exit.</para>
        /// </summary>
        /// <param name="parameters">
        /// <para>An IDictionary-type value object that contains name-value pairs corresponding
        /// to the parameters elements supplied for the Metadata Adapter configuration.
        /// Both names and values are represented as String objects.</para>
        /// <para>The parameters can be supplied in different ways, depending on the way the Remote
        /// Adapters are hosted:</para>
        /// <para>- If the Remote Server is launched through the provided DotNetServer executable:
        /// in the command line, as arguments of the form name=value;</para>
        /// <para>- If the Remote Server consists in a custom application that creates an instance
        /// of the <see cref="Lightstreamer.DotNet.Server.MetadataProviderServer"/> class: through the "AdapterParams" dictionary property
        /// of the <see cref="Lightstreamer.DotNet.Server.MetadataProviderServer"/> instance used.</para>
        /// <para>In both cases more parameters can be added by leveraging the "init_remote" parameter
        /// in the Proxy Adapter configuration.</para>
        /// </param>
        /// <param name="configFile">
        /// <para>The path on the local disk of the Metadata Adapter configuration file.
        /// Can be null if not specified.</para>
        /// <para>The file path can be supplied in different ways, depending on the way the Remote
        /// Adapters are hosted:</para>
        /// <para>- If the Remote Server is launched through the provided DotNetServer executable:
        /// in the command line, with two consecutive arguments, respectively valued with "/config"
        /// and the file path;</para>
        /// <para>- If the Remote Server consists in a custom application that creates an instance
        /// of the <see cref="Lightstreamer.DotNet.Server.MetadataProviderServer"/> class: by assigning the "AdapterConfig" property of the
        /// <see cref="Lightstreamer.DotNet.Server.MetadataProviderServer"/> instance used.</para>
        /// </param>
        /// <exception cref="MetadataProviderException">
        /// in case an error occurs that prevents the correct behavior of the Metadata Adapter.
        /// </exception>
        void Init(IDictionary parameters, string configFile);

		/// <summary>
		/// <para>Called by Lightstreamer Kernel through the Remote Server
		/// as a preliminary check that a user is
		/// enabled to make Requests to the related Data Providers.
		/// It is invoked upon each session request and it is called prior to any
		/// other session-related request. So, any other method with a User
		/// argument can assume that the supplied User argument has already been
		/// checked.</para>
		/// <para>The User authentication should be based on the user and password
		/// arguments supplied by the client. The full report of the request HTTP
		/// headers is also available; they could be used in order to gather
		/// information about the client, but should not be used for authentication,
		/// as they may not be under full control by client code. See also the
		/// discussion about the &lt;use_protected_js&gt; Server configuration
		/// element, if available.</para>
        /// <para>This method runs in the Server authentication thread pool, if defined.</para>
		/// </summary>
		/// <param name="user">A User name.</param>
		/// <param name="password">A password optionally required to validate the User.</param>
        /// <param name="httpHeaders"><para>An IDictionary-type value object that
        /// contains a name-value pair for each header found in the HTTP
        /// request that originated the call. The header names are reported in lower-case form.</para>
        /// <para>For headers defined multiple times, a unique name-value pair is reported,
		/// where the value is a concatenation of all the supplied header values,
		/// separated by a ",".</para>
        /// <para>One pair is added by Lightstreamer Server; the name is �REQUEST_ID�
        /// and the value is a unique id assigned to the client request.</para>
        /// </param>
        /// <exception cref="AccessException">
        /// in case the User name is not known or the supplied password is not correct.
        /// </exception>
        /// <exception cref="CreditsException">
        /// in case the User is known but is not enabled to make further Requests at the moment.
		/// </exception>
		void NotifyUser(string user, string password, IDictionary httpHeaders);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel, through the Remote Server,
        /// instead of calling the 3-arguments version, in case the Server
        /// has been instructed to acquire the client principal from the client TLS/SSL
        /// certificate through the &lt;use_client_auth&gt; configuration flag.</para>
        /// <para>Note that the above flag can be set for each listening port
        /// independently (and it can be set for TLS/SSL ports only), hence, both
        /// overloads may be invoked, depending on the port used by the client.</para>
        /// <para>Also note that in case client certificate authentication is not
        /// forced on a listening port through &lt;force_client_auth&gt;, a client
        /// request issued on that port may not be authenticated, hence it may
        /// have no principal associated. In that case, if &lt;use_client_auth&gt;
        /// is set, this overload will still be invoked, with null principal.</para>
        /// <para>See the base 3-arguments version for other notes.</para>
        /// <br/>
        /// <para><B>Edition Note:</B> https connections is an optional
        /// feature, available depending on Edition and License Type.
		/// To know what features are enabled by your license, please see the License tab of the
		/// Monitoring Dashboard (by default, available at /dashboard).</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <param name="password">A password optionally required to validate the User.</param>
        /// <param name="httpHeaders">An IDictionary-type value object that
        /// contains a name-value pair for each header found in the HTTP
        /// request that originated the call.</param>
        /// <param name="clientPrincipal">the identification name reported in the client
        /// TLS/SSL certificate supplied on the socket connection used to issue the
        /// request that originated the call; it can be null if client has not
        /// authenticated itself or the authentication has failed.</param>
        /// <exception cref="AccessException">
        /// in case the User name is not known or the supplied password is not correct.
        /// </exception>
        /// <exception cref="CreditsException">
        /// in case the User is known but is not enabled to make further Requests at the moment.
        /// </exception>
        void NotifyUser(string user, string password, IDictionary httpHeaders, string clientPrincipal);

        /// <summary>
		/// <para>Called by Lightstreamer Kernel through the Remote Server
        /// to resolve an Item Group name (or Item List specification) supplied in
		/// a Request. The names of the Items in the Group must be returned.
        /// For instance, the client could be allowed to specify the "NASDAQ100"
        /// Group name and, upon that, the list of all items corresponding to the
        /// stocks included in that index could be returned.</para>
		/// <para>Possibly, the content of an Item Group may be dependant on the User
		/// who is issuing the Request or on the specific Session instance.</para>
        ///
        /// <para>When an Item List specification is supplied, it is made of a space-separated
		/// list of the names of the Items in the List. This convention is used
		/// by some of the subscription methods provided by the various client
		/// libraries. The specifications for these methods require that
		/// "A LiteralBasedProvider or equivalent Metadata Adapter is needed
		/// on the Server in order to understand the Request".
		/// When any of these interface methods is used by client code accessing
		/// this Metadata Adapter, the supplied "group" argument should be inspected
		/// as a space-separated list of Item names and an array with these names
		/// in the same order should be returned.</para>
        /// <para>Another typical case is when the same Item has different contents
        /// depending on the User that is issuing the request. On the Data Adapter
        /// side, different Items (one for each User) can be used; nevertheless, on
        /// the client side, the same name can be specified in the subscription
        /// request and the actual user-related name can be determined and returned
        /// here. For instance:
        /// <pre>
        /// if (group.Equals("portfolio")) {
        ///     String itemName = "PF_" + user;
        ///     return new String[] { itemName };
        /// } else if (group.StartsWith("PF_")) {
        ///     // protection from unauthorized use of user-specific items
        ///     throw new ItemsException("Unexpected group name");
        /// }
        /// </pre>
        /// </para>
        /// <para>Obviously, the two above techniques can be combined, hence any
        /// element of an Item List can be replaced with a decorated or alternative
        /// Item name: the related updates will be associated to the original name
        /// used in the Item List specification by client library code.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Data Adapter that supplies the involved Items, if defined.</para>
        /// </summary>
		/// <param name="user">A User name.</param>
		/// <param name="sessionID">The ID of a Session owned by the User.</param>
        /// <param name="group">An Item Group name (or Item List specification).</param>
		/// <returns>An array with the names of the Items in the Group.</returns>
        /// <exception cref="ItemsException">
        /// in case the supplied Item Group name (or Item List specification) is not recognized.
        /// </exception>
		string [] GetItems(string user, string sessionID, string group);

		/// <summary>
		/// <para>Called by Lightstreamer Kernel through the Remote Server
        /// to resolve a Field Schema name (or Field List specification) supplied in
		/// a Request. The names of the Fields in the Schema must be returned.</para>
		/// <para>Possibly, the content of a Field Schema may be dependant on the User
        /// who is issuing the Request, on the specific Session instance or on
        /// the Item Group (or Item List) to which the Request is related.</para>
        ///
        /// <para>When a A Field List specification is supplied, it is made of a space-separated
		/// list of the names of the Fields in the Schema. This convention is used
		/// by some of the subscription methods provided by the various client
		/// libraries. The specifications for these methods require that
		/// "A LiteralBasedProvider or equivalent Metadata Adapter is needed
		/// on the Server in order to understand the Request".
		/// When any of these interface methods is used by client code accessing
		/// this Metadata Adapter, the supplied "schema" argument should be inspected
		/// as a space-separated list of Field names and an array with these names
		/// in the same order should be returned;
		/// returning decorated or alternative Field names is also possible:
		/// they will be associated to the corresponding names used in the
        /// supplied Field List specification by client library code.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Data Adapter that supplies the involved Items, if defined.</para>
        /// </summary>
		/// <param name="user">A User name.</param>
		/// <param name="sessionID">The ID of a Session owned by the User.</param>
        /// <param name="id">The name of the Item Group (or specification of the Item List)
        /// whose Items the Schema is to be applied to.</param>
        /// <param name="schema">A Field Schema name (or Field List specification).</param>
		/// <returns>An array with the names of the Fields in the Schema.</returns>
        /// <exception cref="ItemsException">
        /// in case the supplied Item Group name (or Item List specification) is not recognized.
        /// </exception>
        /// <exception cref="SchemaException">
        /// in case the supplied Field Schema name (or Field List specification) is not recognized.
        /// </exception>
		string [] GetSchema(string user, string sessionID, string id, string schema);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the bandwidth level to be allowed to a User for a push Session.</para>
        /// <para>This method runs in the Server authentication thread pool, if defined.</para>
        /// <br/>
        /// <para><B>Edition Note:</B> Bandwidth Control is an optional
        /// feature, available depending on Edition and License Type.
        /// To know what features are enabled by your license, please see the License tab of the
        /// Monitoring Dashboard (by default, available at /dashboard).</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <returns>The allowed bandwidth, in Kbit/sec. A zero return value means an unlimited bandwidth.</returns>
        double GetAllowedMaxBandwidth(string user);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the ItemUpdate frequency to be allowed to a User for a
        /// specific Item. An unlimited frequency can also be specified. Such filtering applies only to Items
        /// requested with publishing <see cref="Mode"/> MERGE, DISTINCT and COMMAND (in the latter case, the frequency
        /// limitation applies to the UPDATE events for each single key). If an Item is requested with publishing
        /// <see cref="Mode"/> MERGE, DISTINCT or COMMAND and unfiltered dispatching has been specified, then returning any
        /// limited maximum frequency will cause the refusal of the request by Lightstreamer Kernel.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Data Adapter that supplies the involved items, if defined.</para>
        /// <br/>
        /// <para><B>Edition Note:</B> A further global frequency limit could also be imposed
        /// by the Server, depending on Edition and License Type; this specific limit also applies to RAW mode and
        /// to unfiltered dispatching.
        /// To know what features are enabled by your license, please see the License tab of the
        /// Monitoring Dashboard (by default, available at /dashboard).</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <param name="item">An Item Name.</param>
        /// <returns>The allowed Update frequency, in Updates/sec. A zero return value means no frequency
        /// restriction.</returns>
        double GetAllowedMaxItemFrequency(string user, string item);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the maximum size allowed for the buffer internally used to
        /// enqueue subsequent ItemUpdates for the same Item. If this buffer is more than 1 element deep, a short
        /// burst of ItemEvents from the Data Adapter can be forwarded to the Client without losses, though with
        /// some delay. The buffer size is specified in the Request. Its maximum allowed size can be different for
        /// different Users. Such buffering applies only to Items requested with publishing <see cref="Mode"/> MERGE or DISTINCT.
        /// However, if the Item has been requested with unfiltered dispatching, then the buffer size is always
        /// unlimited and buffer size settings are ignored.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Data Adapter that supplies the involved items, if defined.</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <param name="item">An Item Name.</param>
        /// <returns>The allowed buffer size. A zero return value means a potentially unlimited buffer.</returns>
        int GetAllowedBufferSize(string user, string item);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the allowance of a publishing <see cref="Mode"/> for an Item.
        /// A publishing <see cref="Mode"/> can or cannot be allowed depending on the User. The Metadata Adapter should
        /// ensure that conflicting Modes are not both allowed for the same Item (even for different Users),
        /// otherwise some Requests will be eventually refused by Lightstreamer Kernel. The conflicting Modes are
        /// MERGE, DISTINCT and COMMAND.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Data Adapter that supplies the involved items, if defined.</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <param name="item">An Item Name.</param>
        /// <param name="mode">A publishing <see cref="Mode"/>.</param>
        /// <returns>True if the publishing <see cref="Mode"/> is allowed.</returns>
        bool IsModeAllowed(string user, string item, Mode mode);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the allowance of a publishing <see cref="Mode"/> for an Item (for at
        /// least one User). The Metadata Adapter should ensure that conflicting Modes are not both allowed for
        /// the same Item. The conflicting Modes are MERGE, DISTINCT and COMMAND.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Data Adapter that supplies the involved items, if defined.</para>
        /// </summary>
        /// <param name="item">An Item Name.</param>
        /// <param name="mode">A publishing <see cref="Mode"/>.</param>
        /// <returns>True if the publishing <see cref="Mode"/> is allowed.</returns>
        bool ModeMayBeAllowed(string item, Mode mode);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the minimum ItemEvent frequency from the Data Adapter at
        /// which the events for an Item are guaranteed to be delivered to the Clients without loss of information.
        /// In case of an incoming ItemEvent frequency greater than this value, Lightstreamer Kernel may prefilter
        /// the events. Such prefiltering applies only for Items requested with publishing <see cref="Mode"/> MERGE
        /// or DISTINCT.
        /// The frequency set should be greater than the ItemUpdate frequencies allowed to the different Users for
        /// that Item. Moreover, because this filtering is made without buffers, the frequency set should be far
        /// greater than the ItemUpdate frequencies allowed for that Item for which buffering of event bursts is
        /// desired. If an Item is requested with publishing <see cref="Mode"/> MERGE or DISTINCT and 
        /// unfiltered dispatching,
        /// then specifying any limited source frequency will cause the refusal of the request by Lightstreamer Kernel.
        /// This feature is just for ItemEventBuffers protection against Items with a very fast flow on the Data
        /// Adapter and a very slow flow allowed to the Clients. If this is the case, but just a few Clients need
        /// a fast or unfiltered flow for the same MERGE or DISTINCT Item, the use of two differently named Items
        /// that receive the same flow from the Data Adapter is suggested.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Data Adapter that supplies the involved items, if defined.</para>
        /// </summary>
        /// <param name="item">An Item Name.</param>
        /// <returns>The minimum ItemEvent frequency that must be processed without loss of information, in
        /// ItemEvents/sec. A zero return value indicates that incoming ItemEvents must not be prefiltered. If the
        /// ItemEvents frequency for the Item is known to be very low, returning zero allows Lightstreamer Kernel
        /// to save any prefiltering effort.</returns>
        double GetMinSourceFrequency(string item);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the maximum allowed length for a Snapshot of an Item that
        /// has been requested with publishing <see cref="Mode"/> DISTINCT.
        /// In fact, in DISTINCT publishing <see cref="Mode"/>, the Snapshot
        /// for an Item is made by the last events received for the Item and the Client can specify how many events
        /// it would like to receive. Thus, Lightstreamer Kernel must always keep a buffer with some of the last
        /// events received for the Item and the length of the buffer is limited by the value returned by this
        /// method. The maximum Snapshot size cannot be unlimited.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Data Adapter that supplies the involved items, if defined.</para>
        /// </summary>
        /// <param name="item">An Item Name.</param>
        /// <returns>The maximum allowed length for the Snapshot; a zero return value means that no Snapshot
        /// information should be kept.</returns>
        int GetDistinctSnapshotLength(string item);

		/// <summary>
		/// <para>Called by Lightstreamer Kernel through the Remote Server to forward
        /// a message received by a User. The interpretation of the
		/// message is up to the Metadata Adapter. A message can also be refused.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Adapter Set, if defined.</para>
        /// </summary>
		/// <param name="user">A User name.</param>
		/// <param name="sessionID">The ID of a Session owned by the User.</param>
		/// <param name="message">A non-null string.</param>
        /// <exception cref="CreditsException">
        /// in case the User is not enabled to send
		/// the message or the message cannot be correctly managed.
        /// </exception>
        /// <exception cref="NotificationException">
        /// in case something is wrong in the
		/// parameters, such as a nonexistent Session ID.
        /// </exception>
		void NotifyUserMessage(string user, string sessionID, string message);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to check
        /// that a User is enabled to open a new push Session. If the check
        /// succeeds, this also notifies the Metadata Adapter that the Session
        /// is being assigned to the User.</para>
        /// <para>Request context information is also available; this allows for
        /// differentiating group, schema and message management based on specific
        /// Request characteristics.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Adapter Set, if defined.</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <param name="sessionID">The ID of a new Session.</param>
        /// <param name="clientContext">
        /// <para>An IDictionary-type value object that contains name-value
        /// pairs with various information about the request context.
        /// All values are supplied as strings. Information related to a client
        /// connection refers to the HTTP request that originated the call.
        /// Available keys are:</para>
        /// <para>- "REMOTE_IP" - string representation of the remote IP
        /// related to the current connection; it may be a proxy address</para>
        /// <para>- "REMOTE_PORT" - string representation of the remote port
        /// related to the current connection</para>
        /// <para>- "USER_AGENT" - the user-agent as declared in the current
        /// connection HTTP header</para>
        /// <para>- "FORWARDING_INFO" - the content of the X-Forwarded-For
        /// HTTP header related to the current connection; intermediate proxies
        /// usually set this header to supply connection routing information</para>
        /// <para>- "LOCAL_SERVER" - the name of the specific server socket
        /// that handles the current connection, as configured through the
        /// &lt;http_server&gt; or &lt;https_server&gt; element</para>
        /// <para>- "CLIENT_TYPE" - the type of client API in use.
        /// The value may be null for some old client APIs</para>
        /// <para>- "CLIENT_VERSION" - the signature, including version and build number,
        /// of the client API in use.The signature may be only partially complete,
        /// or even null, for some old client APIs and for some custom clients</para>
        /// <para>- "REQUEST_ID" - the same id that has just been supplied
        /// to <see cref="NotifyUser"/> for the current client request instance;
        /// this allows for using local authentication-related details for
        /// the authorization task.</para>
        /// <br/>
        /// Note: the Remote Adapter is responsible for disposing any cached
        /// information in case NotifyNewSession is not called because of any
        /// early error during request management.
        /// </param>
        /// <exception cref="CreditsException">
        /// in case the User is not enabled to open the new Session.
        /// If it's possible that the User would be enabled as soon as
        /// another Session were closed, then a <see cref="ConflictingSessionException"/>
        /// can be thrown, in which the ID of the other Session must be
        /// specified.
        /// In this case, a second invocation of the method with the same
        /// �REQUEST_ID� and a different Session ID will be received.
        /// </exception>
        /// <exception cref="NotificationException">
        /// in case something is wrong in the parameters, such as the ID
        /// of a Session already open for this or a different User.
        /// </exception>
        void NotifyNewSession(string user, string sessionID, IDictionary clientContext);

		/// <summary>
		/// <para>Called by Lightstreamer Kernel through the Remote Server to notify
        /// the Metadata Adapter that a push Session has been closed.</para>
        /// <para>This method is called by the Server asynchronously
        /// and does not consume a pooled thread on the Server.
        /// As a consequence, it is not guaranteed that no more calls related with
        /// this sessionID, like notifyNewTables, notifyTablesClose, and getItems
        /// can occur after its invocation on parallel threads. Accepting them
        /// would have no effect.
        /// However, if the method may have side-effects on the Adapter, like notifyUserMessage,
        /// the Adapter is responsible for checking if the session is still valid.</para>
        /// </summary>
        /// <param name="sessionID">A Session ID.</param>
        /// <exception cref="NotificationException">
        /// in case something is wrong in the parameters, such as the ID of a Session
        /// that is not currently open.
        /// </exception>
        void NotifySessionClose(string sessionID);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to know
        /// whether the Metadata Adapter must or must not be notified any time a Table
        /// (i.e. Subscription) is added or removed from a push Session owned by a supplied User. If this method returns
        /// false, the methods <see cref="NotifyNewTables"/> and <see cref="NotifyTablesClose"/> will never be called for this User, saving
        /// some processing time. In this case, the User will be allowed to add to his Sessions any Tables
        /// (i.e. Subscriptions) he wants.</para>
        /// <para>This method runs in the Server authentication thread pool, if defined.</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <returns>True if the Metadata Adapter must be notified any time a Table (i.e. Subscription)
        /// is added or removed from a Session owned by the User.</returns>
        bool WantsTablesNotification(string user);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to check
        /// that a User is enabled to add some Tables (i.e. Subscriptions) to a push Session.
        /// If the check succeeds, this also notifies the Metadata Adapter that the Tables are being added to the
        /// Session.</para>
        /// <para>The method is invoked only if enabled for the User through <see cref="WantsTablesNotification"/>.</para>
        /// <para>This method runs in the Server thread pool specific
        /// for the Data Adapter that supplies the involved items, if defined.</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <param name="sessionID">The ID of a Session owned by the User.</param>
        /// <param name="tables">An array of <see cref="TableInfo"/> instances, each of them containing the details of a Table
        /// (i.e. Subscription) to be added to the Session.
        /// The elements in the array represent Tables (i.e.: Subscriptions) whose
        /// subscription is requested atomically by the client. A single element
        /// should be expected in the array, unless clients based on a very old
        /// version of a client library or text protocol may be in use.</param>
        /// <exception cref="CreditsException">
        /// in case the User is not allowed to add the specified Tables (i.e. Subscriptions) to the Session.
        /// </exception>
        /// <exception cref="NotificationException">
        /// in case something is wrong in the
        /// parameters, such as the ID of a Session that is not currently open
        /// or inconsistent information about a Table (i.e. Subscription).
        /// </exception>
        void NotifyNewTables(string user, string sessionID, TableInfo [] tables);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel through the Remote Server to notify
        /// the Metadata Adapter that some Tables (i.e. Subscriptions) have been removed from
        /// a push Session.</para>
        /// <para>The method is invoked only if enabled for the User through <see cref="WantsTablesNotification"/>.</para>
        /// <para>This method is called by the Server asynchronously
        /// and does not consume a pooled thread on the Server.</para>
        /// </summary>
        /// <param name="sessionID">A Session ID.</param>
        /// <param name="tables">An array of <see cref="TableInfo"/> instances, each of them containing the details of a Table
        /// (i.e. Subscription) that has been removed from the Session.
        /// The supplied array is in 1:1 correspondance with the array supplied by
        /// <see cref="NotifyNewTables"/> in a previous call;
        /// the correspondance can be recognized by matching the WinIndex property
        /// of the included <see cref="TableInfo"/> objects (if multiple objects are included,
        /// it must be the same for all of them).</param>
        /// <exception cref="NotificationException">
        /// in case something is wrong in the parameters, such as the ID of a Session
        /// that is not currently open or a Table (i.e. Subscription) that is not contained in the Session.
        /// </exception>
        void NotifyTablesClose(string sessionID, TableInfo [] tables);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel to check that a User is enabled to access
        /// the specified MPN device. The success of this method call is
        /// a prerequisite for all MPN operations, including the activation of a
        /// subscription, the deactivation of a subscription, the change of a device
        /// token, etc. Some of these operations have a subsequent specific notification,
        /// i.e. <see cref="NotifyMpnSubscriptionActivation"/> and 
        /// <see cref="NotifyMpnDeviceTokenChange"/>.
        /// </para>
        /// <para>Take particular precautions when authorizing device access, if
        /// possible ensure the user is entitled to the specific platform,
        /// device token and application ID.
        /// </para>
        /// <br/>
        /// <para><B>Edition Note:</B> Push Notifications is an optional feature,
        /// available depending on Edition and License Type.
		/// To know what features are enabled by your license, please see the License tab of the
		/// Monitoring Dashboard (by default, available at /dashboard).</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <param name="sessionID">The ID of a Session owned by the User.</param>
        /// <param name="device">Specifies an MPN device.</param>
        /// <exception cref="CreditsException">if the User is not allowed to access the
        /// specified MPN device in the Session.</exception>
        /// <exception cref="NotificationException">if something is wrong in the parameters,
        /// such as inconsistent information about the device.</exception>
        void NotifyMpnDeviceAccess(string user, string sessionID, MpnDeviceInfo device);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel to check that a User is enabled
        /// to activate a Push Notification subscription.
        /// If the check succeeds, this also notifies the Metadata Adapter that
        /// Push Notifications are being activated.
        /// </para>
        /// <para>Take particular precautions when authorizing subscriptions, if
        /// possible check for validity the trigger expression reported by
        /// MpnSubscriptionInfo.Trigger, as it may contain maliciously
        /// crafted code. The MPN notifiers configuration file contains a first-line
        /// validation mechanism based on regular expression that may also be used
        /// for this purpose.
        /// </para>
        /// <br/>
        /// <para><B>Edition Note:</B> Push Notifications is an optional feature,
        /// available depending on Edition and License Type.
		/// To know what features are enabled by your license, please see the License tab of the
		/// Monitoring Dashboard (by default, available at /dashboard).</para>
        /// </summary>
        /// <param name="user">A User name.</param>
        /// <param name="sessionID">The ID of a Session owned by the User. The session ID is
        /// provided for a thorough validation of the Table information, but Push
        /// Notification subscriptions are persistent and survive the session. Thus,
        /// any association between this Session ID and this Push Notification
        /// subscription should be considered temporary.</param>
        /// <param name="table">A <see cref="TableInfo"/> instance, containing the details of a Table
        /// (i.e.: Subscription) for which Push Notification have to be activated.</param>
        /// <param name="mpnSubscription">An <see cref="MpnSubscriptionInfo"/> instance, containing the
        /// details of a Push Notification to be activated.</param>
        /// <exception cref="CreditsException">if the User is not allowed to activate the
        /// specified Push Notification in the Session.</exception>
        /// <exception cref="NotificationException">if something is wrong in the parameters,
        /// such as inconsistent information about a Table (i.e.: Subscription) or
        /// a Push Notification.</exception>
        void NotifyMpnSubscriptionActivation(string user, string sessionID, TableInfo table, MpnSubscriptionInfo mpnSubscription);

        /// <summary>
        /// <para>Called by Lightstreamer Kernel to check that a User is enabled to change
        /// the token of an MPN device.
        /// If the check succeeds, this also notifies the Metadata Adapter that future
        /// client requests should be issued by specifying the new device token.
        /// </para>
        /// <para>Take particular precautions when authorizing device token changes,
        /// if possible ensure the user is entitled to the new device token.
        /// </para>
        /// <br/>
        /// <para><B>Edition Note:</B> Push Notifications is an optional feature,
        /// available depending on Edition and License Type.
        /// To know what features are enabled by your license, please see the License tab of the
        /// Monitoring Dashboard (by default, available at /dashboard).</para>
        /// </summary>       /// <param name="user">A User name.</param>
        /// <param name="sessionID">A Session ID.</param>
        /// <param name="device">Specifies an MPN device.</param>
        /// <param name="newDeviceToken">The new token being assigned to the device.</param>
        /// <exception cref="CreditsException">if the User is not allowed to change the
        /// specified device token.</exception>
        /// <exception cref="NotificationException">if something is wrong in the parameters,
        /// such as inconsistent information about the device.</exception>
        void NotifyMpnDeviceTokenChange(string user, string sessionID, MpnDeviceInfo device, string newDeviceToken);
	}

	// ////////////////////////////////////////////////////////////////////////
	// Base abstract adapter

	/// <summary>
	/// Provides a default implementation of all the functionality of a Metadata Adapter which allow a simple
	/// default behavior. Overriding this class may facilitate the coding of simple Adapters.
	/// </summary>
	public abstract class MetadataProviderAdapter : IMetadataProvider {

		/// <summary>
		/// No-op initialization.
		/// </summary>
		/// <param name="parameters">Not used.</param>
		/// <param name="configFile">Not used.</param>
        /// <exception cref="MetadataProviderException">
        /// never thrown in this case.
        /// </exception>
		public virtual void Init(IDictionary parameters, string configFile) {}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server
        /// as a preliminary check that a user is
		/// enabled to make Requests to the related Data Providers.
		/// In this default implementation, a simpler 2-arguments version of the
		/// method is invoked, where the httpHeaders argument is discarded.
		/// Note that, for authentication purposes, only the user and password
		/// arguments should be consulted.
		/// </summary>
		/// <param name="user">A User name.</param>
		/// <param name="password">A password optionally required to validate the User.</param>
        /// <param name="httpHeaders">An IDictionary-type value object that
        /// contains a name-value pair for each header found in the HTTP
        /// request that originated the call. Not used.</param>
        /// <exception cref="AccessException">
        /// never thrown in this case.
        /// </exception>
        /// <exception cref="CreditsException">
        /// never thrown in this case.
        /// </exception>
		public virtual void NotifyUser(string user, string password, IDictionary httpHeaders) {
			NotifyUser(user, password);
		}

		/// <summary>
		/// 2-arguments version of the User authentication method. In case the
		/// 3-arguments version of the method is not overridden, this version
		/// of the method is invoked.
		/// In this default implementation, the Metadata Adapter poses no
		/// restriction.
		/// </summary>
		/// <param name="user">Not used.</param>
		/// <param name="password">Not used.</param>
        /// <exception cref="AccessException">
        /// never thrown in this case.
        /// </exception>
        /// <exception cref="CreditsException">
        /// never thrown in this case.
        /// </exception>
        public virtual void NotifyUser(string user, string password) { }

		/// <summary>
		/// <para>Extended version of the User authentication method,
        /// called by Lightstreamer Kernel, through the Remote Server,
        /// in case the Server has been instructed (through the
        /// &lt;use_client_auth&gt; configuration flag) to acquire the
        /// client principal from the client TLS/SSL certificate, if available.</para>
        /// <para>In this default implementation, the base 3-arguments version of
        /// the method is invoked, where the clientPrincipal argument is discarded.
        /// This also ensures backward compatibility with old adapter classes
        /// derived from this one.</para>
		/// </summary>
        ///
        /// <GENERAL_EDITION_NOTE><para><B>Edition Note:</B> https connections is an optional feature,
        /// available depending on Edition and License Type.
		/// To know what features are enabled by your license, please see the License tab of the
		/// Monitoring Dashboard (by default, available at /dashboard).</para></GENERAL_EDITION_NOTE>
        ///
        /// <param name="user">A User name.</param>
		/// <param name="password">A password optionally required to validate the User.</param>
        /// <param name="httpHeaders">An IDictionary-type value object that
        /// contains a name-value pair for each header found in the HTTP
        /// request that originated the call. Not used.</param>
        /// <param name="clientPrincipal">the identification name reported in the client
        /// TLS/SSL certificate supplied on the socket connection used to issue the
        /// request that originated the call; it can be null if client has not
        /// authenticated itself or the authentication has failed. Not used.</param>
        /// <exception cref="AccessException">
        /// never thrown in this case.
        /// </exception>
        /// <exception cref="CreditsException">
        /// never thrown in this case.
        /// </exception>
		public virtual void NotifyUser(string user, string password, IDictionary httpHeaders, string clientPrincipal) {
			NotifyUser(user, password, httpHeaders);
		}

		public abstract string[] GetItems(string user, string sessionID, string id);

		public abstract string[] GetSchema(string user, string sessionID, string id, string schema);

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the bandwidth amount to be allowed to a User for a push
		/// Session. In this default implementation, the Metadata Adapter poses no restriction.
		/// </summary>
		/// <param name="user">Not used.</param>
		/// <returns>Always zero, to mean no bandwidth limit.</returns>
		public virtual double GetAllowedMaxBandwidth(string user) {
			return 0.0;
		}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the ItemUpdate frequency to be allowed to a User for a
		/// specific Item. In this default implementation, the Metadata Adapter poses no restriction; this also
		/// enables unfiltered dispatching for Items subscribed in MERGE or DISTINCT mode.
		/// </summary>
		/// <param name="user">Not used.</param>
		/// <param name="item">Not used.</param>
		/// <returns>Always zero, to mean no frequency limit.</returns>
		public virtual double GetAllowedMaxItemFrequency(string user, string item) {
			return 0.0;
		}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the maximum allowed size of the buffer internally used to
		/// enqueue subsequent ItemUpdates for the same Item. In this default implementation, the Metadata
		/// Adapter poses no restriction.
		/// </summary>
		/// <param name="user">Not used.</param>
		/// <param name="item">Not used.</param>
		/// <returns>Always zero, to mean no size limit.</returns>
		public virtual int GetAllowedBufferSize(string user, string item) {
			return 0;
		}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the allowance of a publishing Mode for an Item. A publishing
		/// Mode can or cannot be allowed depending on the User. In this default implementation, the Metadata
		/// Adapter poses no restriction. As a consequence, conflicting Modes may be both allowed for the same
		/// Item, so the Clients should ensure that the same Item cannot be requested in two conflicting Modes.
		/// </summary>
		/// <param name="user">Not used.</param>
		/// <param name="item">Not used.</param>
		/// <param name="mode">Not used.</param>
		/// <returns>Always true.</returns>
		public virtual bool IsModeAllowed(string user, string item, Mode mode) {
			return true;
		}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the allowance of a publishing Mode for an Item (for at
		/// least one User). In this default implementation, the Metadata Adapter poses no restriction. As a
		/// consequence, conflicting Modes may be both allowed for the same Item, so the Clients should ensure
		/// that the same Item cannot be requested in two conflicting Modes.
		/// </summary>
		/// <param name="item">Not used.</param>
		/// <param name="mode">Not used.</param>
		/// <returns>Always true.</returns>
		public virtual bool ModeMayBeAllowed(string item, Mode mode) {
			return true;
		}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the minimum ItemEvent frequency from the Data Adapter at
		/// which the events for an Item are guaranteed to be delivered to the Clients without loss of
		/// information. In this default implementation, the Metadata Adapter can't set any minimum frequency;
		/// this also enables unfiltered dispatching for Items subscribed in MERGE or DISTINCT mode.
		/// </summary>
		/// <param name="item">Not used.</param>
		/// <returns>Always zero, to mean that incoming ItemEvents must not be prefiltered.</returns>
		public virtual double GetMinSourceFrequency(string item) {
			return 0.0;
		}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to ask
        /// for the maximum allowed length for a Snapshot of an Item that
		/// has been requested with publishing Mode DISTINCT. In this default implementation, 0 events are
		/// specified, so snapshot will not be managed.
		/// </summary>
		/// <param name="item">Not used.</param>
		/// <returns>A value of 0, to mean that no events will be kept in order to satisfy snapshot
		/// requests.</returns>
		public virtual int GetDistinctSnapshotLength(string item) {
			return 0;
		}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to forward
        /// a message received by a User. In this default implementation,
		/// the Metadata Adapter does never accept the message.
		/// </summary>
		/// <param name="user">Not used.</param>
		/// <param name="sessionID">Not used.</param>
		/// <param name="message">Not used.</param>
        /// <exception cref="CreditsException">
        /// always thrown in this case.
        /// </exception>
        /// <exception cref="NotificationException">
        /// never thrown in this case.
        /// </exception>
		public virtual void NotifyUserMessage(string user, string sessionID, string message) {
			throw new CreditsException(0, "Unsupported function");
		}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to check
        /// that a User is enabled to open a new push Session. In this
		/// default implementation, the Metadata Adapter poses no restriction.
		/// </summary>
		/// <param name="user">Not used.</param>
		/// <param name="sessionID">Not used.</param>
        /// <param name="clientContext">Not used.</param>
        /// <exception cref="CreditsException">
        /// never thrown in this case.
        /// </exception>
        /// <exception cref="NotificationException">
        /// never thrown in this case.
        /// </exception>
        public virtual void NotifyNewSession(string user, string sessionID, IDictionary clientContext) { }

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to notify
        /// the Metadata Adapter that a push Session has been closed.
		/// In this default implementation, the Metadata Adapter does nothing, because it doesn't need to
		/// remember the open Sessions.
		/// </summary>
		/// <param name="sessionID">Not used.</param>
        /// <exception cref="NotificationException">
        /// never thrown in this case.
        /// </exception>
		public virtual void NotifySessionClose(string sessionID) { }

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to know
        /// whether the Metadata Adapter must or must not be notified any
		/// time a Table (i.e. Subscription) is added or removed from a push Session owned by a supplied User. In this default
		/// implementation, the Metadata Adapter doesn't require such notifications.
		/// </summary>
		/// <param name="user">Not used.</param>
		/// <returns>Always false, to prevent being notified with notifyNewTables and notifyTablesClose.</returns>
		public virtual bool WantsTablesNotification(string user) {
			return false;
		}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to check
		/// that a User is enabled to add some Tables (i.e. Subscriptions) to a push Session.
		/// In this default implementation, the Metadata Adapter poses no restriction. Unless the
		/// WantsTablesNotification method is overridden, this method will never be called by Lightstreamer Kernel.
		/// </summary>
		/// <param name="user">Not used.</param>
		/// <param name="sessionID">Not used.</param>
		/// <param name="tables">Not used.</param>
        /// <exception cref="CreditsException">
        /// never thrown in this case.
        /// </exception>
        /// <exception cref="NotificationException">
        /// never thrown in this case.
        /// </exception>
		public virtual void NotifyNewTables(string user, string sessionID, TableInfo [] tables) {}

		/// <summary>
		/// Called by Lightstreamer Kernel through the Remote Server to notify
		/// the Metadata Adapter that some Tables (i.e. Subscriptions) have been removed
		/// from a push Session. In this default implementation, the Metadata Adapter does nothing, because it
		/// doesn't need to remember the Tables used. Unless the WantsTablesNotification method is overridden,
		/// this method will never be called by Lightstreamer Kernel.
		/// </summary>
		/// <param name="sessionID">Not used.</param>
		/// <param name="tables">Not used.</param>
        /// <exception cref="NotificationException">
        /// never thrown in this case.
        /// </exception>
		public virtual void NotifyTablesClose(string sessionID, TableInfo [] tables) {}

        /// <summary>
        /// Called by Lightstreamer Kernel to check that a User is enabled to access
        /// the specified MPN device.
        /// In this default implementation, the Metadata Adapter poses no restriction.
        /// </summary>
        ///
        /// <GENERAL_EDITION_NOTE><para><B>Edition Note:</B> Push Notifications is an optional feature,
        /// available depending on Edition and License Type.
		/// To know what features are enabled by your license, please see the License tab of the
		/// Monitoring Dashboard (by default, available at /dashboard).</para></GENERAL_EDITION_NOTE>
        ///
        /// <param name="user">Not used.</param>
        /// <param name="sessionID">Not used.</param>
        /// <param name="device">Not used.</param>
        /// <exception cref="CreditsException">
        /// never thrown in this case.
        /// </exception>
        /// <exception cref="NotificationException">
        /// never thrown in this case.
        /// </exception>
        public virtual void NotifyMpnDeviceAccess(string user, string sessionID, MpnDeviceInfo device) { }

		/// <summary>
        /// Called by Lightstreamer Kernel to check that a User is enabled
        /// to activate a Push Notification subscription.
        /// In this default implementation, the Metadata Adapter poses no restriction.
		/// </summary>
        ///
        /// <GENERAL_EDITION_NOTE><para><B>Edition Note:</B> Push Notifications is an optional feature,
        /// available depending on Edition and License Type.
		/// To know what features are enabled by your license, please see the License tab of the
		/// Monitoring Dashboard (by default, available at /dashboard).</para></GENERAL_EDITION_NOTE>
        ///
        /// <param name="user">Not used.</param>
        /// <param name="sessionID">Not used.</param>
        /// <param name="table">Not used.</param>
        /// <param name="mpnSubscription">Not used.</param>
        /// <exception cref="CreditsException">
        /// never thrown in this case.
        /// </exception>
        /// <exception cref="NotificationException">
        /// never thrown in this case.
        /// </exception>
        public virtual void NotifyMpnSubscriptionActivation(string user, string sessionID, TableInfo table, MpnSubscriptionInfo mpnSubscription) { }

		/// <summary>
        /// Called by Lightstreamer Kernel to check that a User is enabled to change
        /// the token of a MPN device.
        /// In this default implementation, the Metadata Adapter poses no restriction.
		/// </summary>
        ///
        /// <GENERAL_EDITION_NOTE><para><B>Edition Note:</B> Push Notifications is an optional feature, available depending on Edition and License Type.
		/// To know what features are enabled by your license, please see the License tab of the
		/// Monitoring Dashboard (by default, available at /dashboard).</para></GENERAL_EDITION_NOTE>
        ///
        /// <param name="user">Not used.</param>
		/// <param name="sessionID">Not used.</param>
        /// <param name="device">Not used.</param>
        /// <param name="newDeviceToken">Not used.</param>
        /// <exception cref="CreditsException">
        /// never thrown in this case.
        /// </exception>
        /// <exception cref="NotificationException">
        /// never thrown in this case.
        /// </exception>
        public virtual void NotifyMpnDeviceTokenChange(string user, string sessionID, MpnDeviceInfo device, string newDeviceToken) { }
	}
}
