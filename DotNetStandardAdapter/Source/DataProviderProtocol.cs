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
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Collections;

using Lightstreamer.Interfaces.Data;
using Lightstreamer.DotNet.Utils;

namespace Lightstreamer.DotNet.Server {

	internal class DataProviderProtocol : BaseProtocol {

		public const char SUBTYPE_DATAPROVIDER_EXCEPTION= 'D';
		public const char SUBTYPE_FAILURE_EXCEPTION= 'F';
		public const char SUBTYPE_SUBSCRIPTION_EXCEPTION= 'U';

		public const string METHOD_DATA_INIT = "DPI";
		public const string METHOD_SUBSCRIBE= "SUB";
		public const string METHOD_UNSUBSCRIBE= "USB";
		public const string METHOD_FAILURE= "FAL";
		public const string METHOD_END_OF_SNAPSHOT= "EOS";
		public const string METHOD_UPDATE_BY_MAP= "UD3";
		public const string METHOD_CLEAR_SNAPSHOT = "CLS";
		
		// ////////////////////////////////////////////////////////////////////////
		// REMOTE INIT

		public static IDictionary ReadInit(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			IDictionary parameters = new Hashtable();

			String typ= null;
			while (tokenizer.HasMoreTokens()) {
				string headerName;
				string headerValue;
				
				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0])
				{

					case TYPE_STRING:
						string val = tokenizer.NextToken();
						headerName = DecodeStringOld(val);
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_DATA_INIT + " request");
				}

				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0])
				{

					case TYPE_STRING:
						string val = tokenizer.NextToken();
						headerValue = DecodeStringOld(val);
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_DATA_INIT + " request");
				}

				parameters[headerName] = headerValue;
			}

			return parameters;
		}
		
		public static string WriteInit(IDictionary arguments) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_DATA_INIT);

            if (arguments != null)
            {
                Debug.Assert(arguments.Count > 0);
                // protocol version 1.8.1 and above
                IDictionaryEnumerator iter = arguments.GetEnumerator();
                while (iter.MoveNext())
                {
                    sb.Append(SEP);
                    sb.Append(TYPE_STRING);
                    sb.Append(SEP);
                    sb.Append(EncodeStringOld((string)iter.Entry.Key));
                    sb.Append(SEP);
                    sb.Append(TYPE_STRING);
                    sb.Append(SEP);
                    sb.Append(EncodeStringOld((string)iter.Entry.Value));
                }
            }
            else {
                // protocol version 1.8.0
                sb.Append(SEP);
                sb.Append(TYPE_VOID);
            }

            return sb.ToString();
		}

		public static string WriteInit(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_DATA_INIT);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is DataProviderException) sb.Append(SUBTYPE_DATAPROVIDER_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeStringOld(exception.Message));
			
			return sb.ToString();
		}

        // ////////////////////////////////////////////////////////////////////////
        // SUBSCRIBE

        public static SubscribeData ReadSubscribe(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);
			
			SubscribeData data= new SubscribeData();

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) { 
				throw new RemotingException("Token not found while parsing a " + METHOD_SUBSCRIBE + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					string itemName= tokenizer.NextToken();
					data.ItemName= DecodeString(itemName);
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_SUBSCRIBE + " request");
			}

			return data;
		}

		public static string WriteSubscribe() {
			StringBuilder sb= new StringBuilder();
			
			sb.Append(METHOD_SUBSCRIBE);
			sb.Append(SEP);
			sb.Append(TYPE_VOID);
			
			return sb.ToString();
		}

		public static string WriteSubscribe(Exception exception) {
			StringBuilder sb= new StringBuilder();
			
			sb.Append(METHOD_SUBSCRIBE);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is SubscriptionException) sb.Append(SUBTYPE_SUBSCRIPTION_EXCEPTION);
			if (exception is FailureException) sb.Append(SUBTYPE_FAILURE_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));
			
			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// UNSUBSCRIBE

		public static string ReadUnsubscribe(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) { 
				throw new RemotingException("Token not found while parsing a " + METHOD_UNSUBSCRIBE + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					string val= tokenizer.NextToken();
					return DecodeString(val);
				
				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_UNSUBSCRIBE + " request");
			}
		}

		public static string WriteUnsubscribe() {
			StringBuilder sb= new StringBuilder();
			
			sb.Append(METHOD_UNSUBSCRIBE);
			sb.Append(SEP);
			sb.Append(TYPE_VOID);
			
			return sb.ToString();
		}

		public static string WriteUnsubscribe(Exception exception) {
			StringBuilder sb= new StringBuilder();
			
			sb.Append(METHOD_UNSUBSCRIBE);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is SubscriptionException) sb.Append(SUBTYPE_SUBSCRIPTION_EXCEPTION);
			if (exception is FailureException) sb.Append(SUBTYPE_FAILURE_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));
			
			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// FAILURE
		
		public static string WriteFailure(Exception exception) {
			StringBuilder sb= new StringBuilder();
			
			sb.Append(METHOD_FAILURE);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));
			
			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// END OF SNAPSHOT

		public static string WriteEndOfSnapshot(string itemName, string requestID)
		{
			StringBuilder sb= new StringBuilder();
			
			sb.Append(METHOD_END_OF_SNAPSHOT);
			sb.Append(SEP);
			sb.Append(TYPE_STRING);
			sb.Append(SEP);
			sb.Append(EncodeString(itemName));
			sb.Append(SEP);
			sb.Append(TYPE_STRING);
			sb.Append(SEP);
			sb.Append(requestID);
			
			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// UPDATE (String itemName, IndexedItemEvent event, boolean isSnapshot)

		public static string WriteUpdateByIndexedEvent(string itemName, string requestID, IIndexedItemEvent itemEvent, bool isSnapshot)
		{
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_UPDATE_BY_MAP); // since we will write it as a set of key-value pairs
			sb.Append(SEP);
			sb.Append(TYPE_STRING);
			sb.Append(SEP);
			sb.Append(EncodeString(itemName));
			sb.Append(SEP);
			sb.Append(TYPE_STRING);
			sb.Append(SEP);
			sb.Append(requestID);
			sb.Append(SEP);
			sb.Append(TYPE_BOOLEAN);
			sb.Append(SEP);
			sb.Append(isSnapshot ? VALUE_TRUE : VALUE_FALSE);
			
			for (int i= 0; i <= itemEvent.GetMaximumIndex(); i++) {
				sb.Append(SEP);
				sb.Append(TYPE_INT);
				sb.Append(SEP);
				sb.Append(i);
				
				sb.Append(SEP);
				sb.Append(TYPE_STRING);
				sb.Append(SEP);
				sb.Append(EncodeString(itemEvent.GetName(i)));

				Object value = itemEvent.GetValue(i);
				if (value == null) {
					// with no type information, let's handle it as a string
					sb.Append(SEP);
					sb.Append(TYPE_STRING);
					sb.Append(SEP);
					sb.Append(EncodeString(null));

				} else if (value is string) {
					sb.Append(SEP);
					sb.Append(TYPE_STRING);
					sb.Append(SEP);
					sb.Append(EncodeString((string) value));

				} else if (value is byte []) {
					sb.Append(SEP);
					sb.Append(TYPE_STRING);
					sb.Append(SEP);
					sb.Append(EncodeBytesAsString((byte []) value));
				
				} else throw new RemotingException("Found value '" + value.ToString() + "' of an unsupported type while building a " + METHOD_UPDATE_BY_MAP + " request");
			}
			
			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// UPDATE (String itemName, ItemEvent event, boolean isSnapshot)

		public static string WriteUpdateByEvent(string itemName, string requestID, IItemEvent itemEvent, bool isSnapshot) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_UPDATE_BY_MAP); // since we will write it as a set of key-value pairs
			sb.Append(SEP);
			sb.Append(TYPE_STRING);
			sb.Append(SEP);
			sb.Append(EncodeString(itemName));
			sb.Append(SEP);
			sb.Append(TYPE_STRING);
			sb.Append(SEP);
			sb.Append(requestID);
			sb.Append(SEP);
			sb.Append(TYPE_BOOLEAN);
			sb.Append(SEP);
			sb.Append(isSnapshot ? VALUE_TRUE : VALUE_FALSE);
			
			IEnumerator iter= itemEvent.GetNames();
			while (iter.MoveNext()) {
				sb.Append(SEP);
				sb.Append(TYPE_STRING);
				sb.Append(SEP);
				sb.Append(EncodeString((string) iter.Current));

				Object value = itemEvent.GetValue((string) iter.Current);
				if (value == null) {
					// with no type information, let's handle it as a string
					sb.Append(SEP);
					sb.Append(TYPE_STRING);
					sb.Append(SEP);
					sb.Append(EncodeString(null));

				} else if (value is string) {
					sb.Append(SEP);
					sb.Append(TYPE_STRING);
					sb.Append(SEP);
					sb.Append(EncodeString((string) value));

				} else if (value is byte []) {
					sb.Append(SEP);
					sb.Append(TYPE_STRING);
					sb.Append(SEP);
					sb.Append(EncodeBytesAsString((byte[])value));
				
				} else throw new RemotingException("Found value '" + value.ToString() + "' of an unsupported type while building a " + METHOD_UPDATE_BY_MAP + " request");
			}
			
			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// UPDATE (String itemName, Map event, boolean isSnapshot)

		public static string WriteUpdateByMap(string itemName, string requestID, IDictionary itemEvent, bool isSnapshot)
		{
			StringBuilder sb= new StringBuilder();
			
			sb.Append(METHOD_UPDATE_BY_MAP);
			sb.Append(SEP);
			sb.Append(TYPE_STRING);
			sb.Append(SEP);
			sb.Append(EncodeString(itemName)); 
			sb.Append(SEP);
			sb.Append(TYPE_STRING);
			sb.Append(SEP);
			sb.Append(requestID);
			sb.Append(SEP);
			sb.Append(TYPE_BOOLEAN);
			sb.Append(SEP);
			sb.Append(isSnapshot ? VALUE_TRUE : VALUE_FALSE);
			
			foreach (string name in itemEvent.Keys) {
				sb.Append(SEP);
				sb.Append(TYPE_STRING);
				sb.Append(SEP);
				sb.Append(EncodeString(name));

				Object value = itemEvent[name];
				if (value == null) {
					// with no type information, let's handle it as a string
					sb.Append(SEP);
					sb.Append(TYPE_STRING);
					sb.Append(SEP);
					sb.Append(EncodeString(null));

				} else if (value is string) {
					sb.Append(SEP);
					sb.Append(TYPE_STRING);
					sb.Append(SEP);
					sb.Append(EncodeString((string) value));

				} else if (value is byte []) {
					sb.Append(SEP);
					sb.Append(TYPE_STRING);
					sb.Append(SEP);
					sb.Append(EncodeBytesAsString((byte []) value));

				} else throw new RemotingException("Found value '" + value.ToString() + "' of an unsupported type while building a " + METHOD_UPDATE_BY_MAP + " request");
			}
			
			return sb.ToString();
		}

        // ////////////////////////////////////////////////////////////////////////
        // CLEAR SNAPSHOT

        public static string WriteClearSnapshot(string itemName, string requestID)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(METHOD_CLEAR_SNAPSHOT);
            sb.Append(SEP);
            sb.Append(TYPE_STRING);
            sb.Append(SEP);
            sb.Append(EncodeString(itemName));
            sb.Append(SEP);
            sb.Append(TYPE_STRING);
            sb.Append(SEP);
            sb.Append(requestID);

            return sb.ToString();
        }
    }
	
	// ////////////////////////////////////////////////////////////////////////
	// Support classes

	internal class SubscribeData {
		public string ItemName;
	}

	internal class SimpleIndexedItemEvent : IIndexedItemEvent {
		private IList _names;
		private IList _values;
		private IDictionary _nameMap;
		
		public SimpleIndexedItemEvent() {
			_names= new ArrayList();
			_values= new ArrayList();
			_nameMap= new Hashtable();
		}

		public int GetIndex(string name) {
			if (!_nameMap.Contains(name)) return -1;
			return (int) _nameMap[name];
		}

		public int GetMaximumIndex() {
			return (_names.Count -1);
		}

		public string GetName(int index) {
			return (string) _names[index];
		}

		public Object GetValue(int index) {
			return _values[index];
		}
		
		public void Add(int index, string name, Object value) {
			if (index >= _names.Count) {
				for (int i= _names.Count; i < index; i++) {
					_names.Add(null);
					_values.Add(null);
				}
				_names.Add(name);
				_values.Add(value);
			} else {
				_names[index]= name;
				_values[index]= value;
			}
			_nameMap[name]= index;
		}
	}

	internal class SimpleItemEvent : IItemEvent {
		private IDictionary _fields;
		
		public SimpleItemEvent() {
			_fields= new Hashtable();
		}

		public IEnumerator GetNames() {
			return _fields.Keys.GetEnumerator();
		}

		public Object GetValue(String name) {
			return _fields[name];
		}
		
		public void Add(String name, Object value) {
			_fields[name]= value;
		}
	}
}
