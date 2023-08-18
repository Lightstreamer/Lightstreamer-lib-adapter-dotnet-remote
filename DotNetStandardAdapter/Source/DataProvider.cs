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

namespace Lightstreamer.Interfaces.Data {

	// ////////////////////////////////////////////////////////////////////////
	// Exceptions

	/// <summary>
	/// Thrown by the init method in DataProvider if there is some problem that prevents the correct behavior 
	/// of the Data Adapter. If this exception occurs, Lightstreamer Kernel must give up the startup.
	/// </summary>
	public class DataProviderException : DataException {
		
		/// <summary>
		/// Constructs a DataProviderException with a supplied error message text.
		/// </summary>
		/// <param name="msg">The detail message.</param>
		public DataProviderException(string msg) : base(msg) {}
	}
	
	/// <summary>
	/// Thrown by the subscribe and unsubscribe methods in DataProvider if the method execution has caused 
	/// a severe problem that can compromise future operation of the Data Adapter.
	/// </summary>
	public class FailureException : DataException {
		
		/// <summary>
		/// Constructs a FailureException with a supplied error message text.
		/// </summary>
		/// <param name="msg">The detail message.</param>
		public FailureException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown by the subscribe and unsubscribe methods in DataProvider if the request cannot be satisfied.
	/// </summary>
	public class SubscriptionException : DataException {
		
		/// <summary>
		/// Constructs a SubscriptionException with a supplied error message text.
		/// </summary>
		/// <param name="msg">The detail message.</param>
		public SubscriptionException(string msg) : base(msg) {}
	}

	// ////////////////////////////////////////////////////////////////////////
	// Constants

	/// <summary>
	/// Contains constants for the special field names and field values recognized by the Server.
	/// </summary>
	public class DataProviderConstants
	{
		
		/// <value>
		/// Constant that can be used as field name for the "key" field in Items to be processed in COMMAND mode.
		/// </value>
		public const string KEY_FIELD = "key";

		/// <value>
		/// Constant that can be used as field name for the "command" field in Items to be processed in 
		/// COMMAND mode.
		/// </value>
		public const string COMMAND_FIELD = "command";
		
		/// <value>
		/// Constant that can be used as the "ADD" value for the "command" fields of Items to be processed in 
		/// COMMAND mode.
		/// </value>
		public const string ADD_COMMAND = "ADD";

		/// <value>
		/// Constant that can be used as the "UPDATE" value for the "command" fields of Items to be processed 
		/// in COMMAND mode.
		/// </value>
		public const string UPDATE_COMMAND = "UPDATE";

		/// <value>
		/// Constant that can be used as the "DELETE" value for the "command" fields of Items to be processed 
		/// in COMMAND mode.
		/// </value>
		public const string DELETE_COMMAND = "DELETE";
	}

	// ////////////////////////////////////////////////////////////////////////
	// Interfaces

	/// <summary>
	/// Provides to the Data Adapter a base interface for creating Item Events
	/// in order to send updates to Lightstreamer Kernel.
	/// An IItemEvent object contains the new values and, in some cases, the current 
	/// values of the Fields of an Item. All implementation methods should be nonblocking.
	/// </summary>
	/// <remarks>
	/// The class is deprecated. Use a IDictionary to supply field values to IItemEventListener's Update.
	/// </remarks>
	[Obsolete("The class is deprecated. Use a IDictionary to supply field values to IItemEventListener's Update.")]
	public interface IItemEvent {
	
		/// <summary>
		/// Returns an enumerator to browse the names of the supplied Fields, expressed as String.
		/// </summary>
		/// <returns>An enumerator.</returns>
		IEnumerator GetNames();

		/// <summary>
		/// Returns the value of a named Field (null is a legal value too). Returns null if the Field is not 
		/// reported in the Item Event. The value should be expressed as a String;
		/// the use of a byte array, to supply a string encoded in the ISO-8859-1 (ISO-LATIN-1)
		/// character set, is also allowed, but it has been deprecated.
		/// The Remote Server, will call this method only once for each Field.
		/// </summary>
		/// <param name="name">A Field name.</param>
		/// <returns>A String containing the Field value, or null. A byte array is also accepted, but deprecated.</returns>
		Object GetValue(string name);
	}

	/// <summary>
	/// Provides to the Data Adapter an alternative interface for creating Item Events
	/// in order to send updates to Lightstreamer Kernel.
	/// In this event, a name-index association is defined for all fields. These indexes will be used
	/// by the Remote Server to iterate through all the fields. Some indexes may not be associated to
	/// fields in the event, but the number of such holes should be small. The name-index associations are local 
	/// to the event and may be different even across events belonging to the same Item.
	/// </summary>
	/// <remarks>
	/// The class is deprecated. Use a IDictionary to supply field values to IItemEventListener's Update.
	/// </remarks>
   [Obsolete("The class is deprecated. Use a IDictionary to supply field values to IItemEventListener's Update.")]
	public interface IIndexedItemEvent {

		/// <summary>
		/// Returns the maximum index for the fields in the event. The event cannot be empty, so the maximum 
		/// Index must always exist.
		/// </summary>
		/// <returns>A 0-based index.</returns>
		int GetMaximumIndex();

		/// <summary>
		/// Returns the index of a named Field. Returns -1 if such a field is not reported in this event. 
		/// So, the implementation must be very fast.
		/// </summary>
		/// <param name="name">A Field name.</param>
		/// <returns>A 0-based index for the field or -1. The index must not be greater than the maximum index 
		/// returned by getMaximumIndex().</returns>
		int GetIndex(string name);

		/// <summary>
		/// Returns the name of a Field whose index is supplied. Returns null if the Field is not reported in 
		/// this event.
		/// </summary>
		/// <param name="index">A Field index.</param>
		/// <returns>The name of a Field, or null.</returns>
		string GetName(int index);

		/// <summary>
		/// Returns the value of a field whose index is supplied (null is a legal value too). Returns null if 
		/// the Field is not reported in the Item Event. The value should be expressed as a String;
		/// the use of a byte array, to supply a string encoded in the ISO-8859-1 (ISO-LATIN-1)
		/// character set, is also allowed, but it has been deprecated.
		/// </summary>
		/// <param name="index">A Field index.</param>
		/// <returns>A String containing the Field value, or null. A byte array is also accepted, but deprecated.</returns>
		Object GetValue(int index);
	}

	/// <summary>
	/// Used by Lightstreamer Kernel to receive the update events and any asynchronous severe error notification 
	/// from the Data Adapter. The listener instance is supplied to the Data Adapter by Lightstreamer Kernel
	/// (through the Remote Server) through a SetListener call.
	/// Update events are specified through maps (i.e. IDictionary) that associate fields and values.
	/// Depending on the kind of subscription, the mapping for fields unchanged since the previous update can be omitted.
	/// Some alternative methods to supply update events are available, but they have been deprecated.
	/// Field values should be expressed as String; the use of byte arrays is also allowed, but it has been deprecated.
	/// </summary>
	public interface IItemEventListener {

		/// <summary>
		/// Called by a Data Adapter to send an Item Event to Lightstreamer Kernel when the Item Event is 
		/// implemented as an <see cref="IItemEvent"/> instance.
		/// </summary>
		/// <param name="itemName">The name of the Item whose values are carried by the Item Event.</param>
		/// <param name="itemEvent">An <see cref="IItemEvent"/> instance.</param>
		/// <param name="isSnapshot">True if the Item Event carries the Item Snapshot.</param>
		/// <remarks>
		/// The method is deprecated. Use the IDictionary version to supply field values.
		/// </remarks>
		[Obsolete("The method is deprecated. Use the IDictionary version to supply field values.")]
		void Update(string itemName, IItemEvent itemEvent, bool isSnapshot);

		/// <summary>
		/// <para>Called by a Data Adapter to send an Item Event to Lightstreamer Kernel when the Item Event is 
		/// implemented as a IDictionary instance.</para>
		/// <para>The Remote Adapter should ensure that, after an Unsubscribe call
		/// for the Item has returned, no more Update calls are issued, until
		/// requested by a new subscription for the same Item.
		/// This assures that, upon a new subscription for the Item, no trailing
		/// events due to the previous subscription can be received by the Remote
		/// Server.
		/// Note that the method is nonblocking; moreover, it only takes locks
		/// to first order mutexes; so, it can safely be called while holding
		/// a custom lock.</para>
		/// </summary>
		/// <param name="itemName">The name of the Item whose values are carried by the Item Event.</param>
		/// <param name="itemEvent">A IDictionary instance, in which Field names are associated to Field values. 
		/// A value should be expressed as a String; the use of a byte array, to supply a string encoded
		/// in the ISO-8859-1 (ISO-LATIN-1) character set, is also allowed, but it has been deprecated.
		/// A Field value can be null or missing if the Field is not to be reported in the event.</param>
		/// <param name="isSnapshot">True if the Item Event carries the Item Snapshot.</param>
		void Update(string itemName, IDictionary itemEvent, bool isSnapshot);

		/// <summary>
		/// Called by a Data Adapter to send an Item Event to Lightstreamer Kernel when the Item Event is 
		/// implemented as an <see cref="IIndexedItemEvent"/> instance.
		/// </summary>
		/// <param name="itemName">The name of the Item whose values are carried by the Item Event.</param>
		/// <param name="itemEvent">An <see cref="IIndexedItemEvent"/> instance.</param>
		/// <param name="isSnapshot">True if the Item Event carries the Item Snapshot.</param>
		/// <remarks>
		/// The method is deprecated. Use the IDictionary version to supply field values.
		/// </remarks>
		[Obsolete("The method is deprecated. Use the IDictionary version to supply field values.")]
		void Update(string itemName, IIndexedItemEvent itemEvent, bool isSnapshot);

		/// <summary>
		/// <para>Called by a Data Adapter to signal to Lightstreamer Kernel that no more Item Event belonging to the 
		/// Snapshot are expected for an Item. This call is optional, because the Snapshot completion can also be 
		/// inferred from the isSnapshot flag in the update calls. However, this call allows 
		/// Lightstreamer Kernel to be informed of the Snapshot completion before the arrival of the first 
		/// non-snapshot event. Calling this function is recommended if the Item is to be subscribed in DISTINCT 
		/// mode. In case the Data Adapter returned false to IsSnapshotAvailable for the same Item, this function 
		/// should not be called.</para>
		/// <para>The Remote Adapter should ensure that, after an Unsubscribe call
		/// for the Item has returned, a possible pending EndOfSnapshot call related
		/// with the previous subscription request is no longer issued.
		/// This assures that, upon a new subscription for the Item, no trailing
		/// events due to the previous subscription can be received by the Remote
		/// Server.
		/// Note that the method is nonblocking; moreover, it only takes locks
		/// to first order mutexes; so, it can safely be called while holding
		/// a custom lock.</para>
		/// </summary>
		/// <param name="itemName">The name of the Item whose snapshot has been completed.</param>
		void EndOfSnapshot(string itemName);

		/// <summary>
		/// <para>Called by a Data Adapter to signal to Lightstreamer Kernel that the
		/// current Snapshot of the Item has suddenly become empty. More precisely:</para>
		/// <para>- for subscriptions in MERGE mode, the current state of the Item will
		/// be cleared, as though an update with all fields valued as null were issued;</para>
		/// <para>- for subscriptions in COMMAND mode, the current state of the Item
		/// will be cleared, as though a DELETE event for each key were issued;</para>
		/// <para>- for subscriptions in DISTINCT mode, a suitable notification that
		/// the Snapshot for the Item should be cleared will be sent to all the
		/// clients currently subscribed to the Item (clients based on some old
		/// client library versions may not be notified); at the same time,
		/// the current recent update history kept by the Server for the Item
		/// will be cleared and this will affect the Snapshot for new subscriptions;</para>
		/// <para>- for subscriptions in RAW mode, there will be no effect.</para>
		/// <para>Note that this is a real-time event, not a Snapshot event; hence,
		/// in order to issue this call, it is not needed that the Data Adapter
		/// has returned true to IsSnapshotAvailable for the specified Item;
		/// moreover, if invoked while the Snapshot is being supplied, the Kernel
		/// will infer that the Snapshot has been completed.</para>
		/// <para>The Adapter should ensure that, after an unsubscribe call for the
		/// Item has returned, a possible pending ClearSnapshot call related with
		/// the previous subscription request is no longer issued.
		/// This assures that, upon a new subscription for the Item, no trailing
		/// events due to the previous subscription can be received by the Kernel.
		/// Note that the method is nonblocking; moreover, it only takes locks
		/// to first order mutexes; so, it can safely be called while holding
		/// a custom lock.
		/// </para>
		/// </summary>
		/// <param name="itemName">The name of the Item whose Snapshot has become empty.</param>
		void ClearSnapshot(string itemName);

		/// <summary>
		/// Called by a Data Adapter to notify Lightstreamer Kernel of the occurrence of a severe problem that 
		/// can compromise future operation of the Data Adapter.
		/// </summary>
		/// <param name="exception">Any Excetion object, with the description of the problem.</param>
		void Failure(Exception exception);
	}

	/// <summary>
	/// <para>Provides an interface to be implemented by a Remote Data Adapter in order
	/// to attach a Data Provider to Lightstreamer.
	/// A single instance of a Remote Data Adapter is created by Lightstreamer
	/// through the launch of a Remote Server, based on configured class name and parameters.
	/// For this purpose, any Remote Data Adapter must provide a void constructor.
	/// Alternatively, an instance of a Remote Data Adapter is supplied to Lightstreamer
	/// programmatically through a <see cref="Lightstreamer.DotNet.Server.DataProviderServer"/> instance.
	/// After initialization, Lightstreamer sets itself
	/// as the Remote Data Adapter listener, by calling the setListener method.</para>
	/// <para>Data Providers are used by Lightstreamer Kernel to obtain all data to be
	/// pushed to the Clients. Any Item requested by a Client must refer to one
	/// supplied by the configured Data Adapters.</para>
	/// <para>A Data Provider supplies data in a publish/subscribe way. Lightstreamer
	/// asks for data by calling the subscribe and unsubscribe methods for
	/// various Items and the Data Adapter sends ItemEvents to its listener
	/// in an asynchronous way.</para>
	/// <para>A Data Adapter can also support Snapshot management. Upon subscription
	/// to an Item, the current state of the Item data can be sent to the Server
	/// before the updates. This allows the Server to maintain the Item state,
	/// by integrating the new ItemEvents into the state (in a way that depends
	/// on the Item type) and to make this state available to the Clients.</para>
	/// <para>Note that the interaction between the Server and the Data Adapter and the
	/// interaction between the Server and any Client are independent activities.
	/// As a consequence, the very first ItemEvents sent by the Data Adapter to
	/// the Server for an Item just subscribed to might be processed before the
	/// Server starts feeding any client, even the client that caused the
	/// subscription to the Item to be invoked;
	/// then, such events would not be forwarded to any client.
	/// If it is desirable that a client receives all the ItemEvents that have
	/// been produced for an Item by the Data Adapter since subscription time,
	/// then the support for the Item Snapshot can be leveraged.</para>
	/// <para>Lightstreamer ensures that calls to subscribe and unsubscribe for
	/// the same Item will be interleaved, without redundant calls; whenever
	/// subscribe throws an exception, the corresponding unsubscribe call is not
	/// issued.</para>
	/// </summary>
	public interface IDataProvider {

		/// <summary>
		/// <para>Called by the Remote Server to provide
		/// initialization information to the Data Adapter.
		/// The call must not be blocking; any polling cycle or similar must be started in a different thread.
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
		/// to the parameters elements supplied for the Data Adapter configuration.
		/// Both names and values are represented as String objects.</para>
		/// <para>The parameters can be supplied in different ways, depending on the way the Remote
		/// Adapters are hosted:</para>
		/// <para>- If the Remote Server is launched through the provided DotNetServer executable:
		/// in the command line, as arguments of the form name=value;</para>
		/// <para>- If the Remote Server consists in a custom application that creates an instance
		/// of the <see cref="Lightstreamer.DotNet.Server.DataProviderServer"/> class: through the "AdapterParams" dictionary property
		/// of the <see cref="Lightstreamer.DotNet.Server.DataProviderServer"/> instance used.</para>
		/// <para>In both cases more parameters can be added by leveraging the "init_remote" parameter
		/// in the Proxy Adapter configuration.</para>
		/// </param>
		/// <param name="configFile">
		/// <para>The path on the local disk of the Data Adapter configuration file.
		/// Can be null if not specified.</para>
		/// <para>The file path can be supplied in different ways, depending on the way the Remote
		/// Adapters are hosted:</para>
		/// <para>- If the Remote Server is launched through the provided DotNetServer executable:
		/// in the command line, with two consecutive arguments, respectively valued with "/config"
		/// and the file path;</para>
		/// <para>- If the Remote Server consists in a custom application that creates an instance
		/// of the <see cref="Lightstreamer.DotNet.Server.DataProviderServer"/> class: by assigning the "AdapterConfig" property of the
		/// <see cref="Lightstreamer.DotNet.Server.DataProviderServer"/> instance used.</para>
		/// </param>
		/// <exception cref="DataProviderException">
		/// in case an error occurs that prevents the correct behavior of the Data Adapter.
		/// </exception>
		void Init(IDictionary parameters, string configFile);

		/// <summary>
		/// Called by the Remote Server to provide
        /// a listener to receive the Item Events carrying data and 
		/// asynchronous error notifications for Lightstreamer Kernel.
        /// The listener is set before any subscribe is called and is never changed.
		/// </summary>
		/// <param name="eventListener">A listener.</param>
		void SetListener(IItemEventListener eventListener);

		/// <summary>
		/// <para>Called by Lightstreamer Remote Server to request data for an Item. If the
		/// request succeeds, the Remote Data Adapter can start sending an ItemEvent
		/// to the listener for any update of the Item value. Before sending the
		/// updates, the Remote Data Adapter may optionally send one or more ItemEvents
		/// to supply the current Snapshot.</para>
		/// <para>The general rule to be followed for event dispatching is:
		/// <code>
		///	  if IsSnapshotAvailable(itemName) == true
		///		   SNAP* [EOS] UPD*
		///	  else
		///		   UPD*
		/// </code>
		/// where:</para>
		/// <para>- SNAP represents an Update call with the isSnapshot flag set to true</para>
		/// <para>- EOS represents an EndOfSnapshot call</para>
		/// <para>- UPD represents an Update call with the isSnapshot flag set to false;
		/// in this case, the special ClearSnapshot call can also be issued.</para>
		/// <para> The composition of the snapshot depends on the <see cref="Lightstreamer.Interfaces.Metadata.Mode"/> in which the Item
		/// is to be processed. In particular, for MERGE mode, the snapshot
		/// consists of one event and the first part of the rule becomes:
		/// <code>
		///		   [SNAP] [EOS] UPD*
		/// </code>
		/// where a missing snapshot is considered as an empty snapshot.</para>
		/// <para>If an Item can be requested only in RAW mode, then <see cref="IsSnapshotAvailable"/>
		/// should always return false; anyway, when an Item is requested in
		/// RAW mode, any snapshot is discarded.</para>
		/// <para>Note that calling EndOfSnapshot is not mandatory; however, not
		/// calling it in DISTINCT or COMMAND mode may cause the server to keep
		/// the snapshot and forward it to the clients only after the first
		/// non-shapshot event has been received. The same happens for MERGE mode
		/// if neither the snapshot nor the EndOfSnapshot call are supplied.</para>
		/// <para>Unexpected snapshot events are converted to non-snapshot events
		/// (but for RAW mode, where they are ignored); unexpected EndOfSnapshot
		/// calls are ignored.</para>
		/// <para>The method can be blocking, but, as the Proxy Adapter
		/// implements subscribe and unsubscribe asynchronously,
		/// subsequent subscribe-unsubscribe-subscribe-unsubscribe requests
		/// can still be issued by Lightstreamer Server to the Proxy Adapter.
		/// When this happens, the requests may be queued on the Remote Adapter,
		/// hence some Subscribe calls may be delayed.</para>
		/// </summary>
		/// <param name="itemName">Name of an Item.</param>
		/// <exception cref="SubscriptionException">
		/// in case the request cannot be satisfied.
		/// </exception>
		/// <exception cref="FailureException">
		/// in case the method execution has caused
		/// a severe problem that can compromise future operation of the Data Adapter.
		/// </exception>
		void Subscribe(string itemName);

		/// <summary>
		/// <para>Called by Lightstreamer Kernel through the Remote Server
		/// to end a previous request of data for an Item.
		/// After the call has returned, no more ItemEvents for the Item
		/// should be sent to the listener until requested by a new subscription
		/// for the same Item.</para>
		/// <para>The method can be blocking, but, as the Proxy Adapter
		/// implements subscribe and unsubscribe asynchronously,
		/// subsequent subscribe-unsubscribe-subscribe-unsubscribe requests
		/// can still be issued by Lightstreamer Server to the Proxy Adapter.
		/// When this happens, the requests may be queued on the Remote Adapter,
		/// hence some <see cref="Subscribe"/> calls may be delayed.</para>
		/// </summary>
		/// <param name="itemName">Name of an Item.</param>
		/// <exception cref="SubscriptionException">
		/// in case the request cannot be satisfied.
		/// </exception>
		/// <exception cref="FailureException">
		/// in case the method execution has caused
		/// a severe problem that can compromise future operation of the Data Adapter.
		/// </exception>
		void Unsubscribe(string itemName);

		/// <summary>
		/// <para>Called by Lightstreamer Kernel through the Remote Server
        /// to know whether the Data Adapter, after a subscription for an Item, 
		/// will send some Snapshot Item Events before sending the updates. An Item Snapshot can be represented 
		/// by zero, one or more Item Events, also depending on the Item type. The decision whether to supply or 
		/// not to supply Snapshot information is entirely up to the Data Adapter.</para>
		/// <para>The method should be nonblocking. The availability of the snapshot for an
		/// Item should be a known architectural property. When the snapshot, though expected,
		/// cannot be obtained at subscription time, then it can only be considered as empty.</para>
		/// </summary>
		/// <param name="itemName">Name of an Item.</param>
		/// <returns>True if Snapshot information will be sent for this Item before the updates.</returns>
        /// <exception cref="SubscriptionException">
        /// in case the Data Adapter is unable to answer to the request.
        /// </exception>
		bool IsSnapshotAvailable(string itemName);
	}
	
}
