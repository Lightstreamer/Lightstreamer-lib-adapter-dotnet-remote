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
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Globalization;

using Lightstreamer.Interfaces.Metadata;
using Lightstreamer.DotNet.Utils;

namespace Lightstreamer.DotNet.Server {

	internal class MetadataProviderProtocol : BaseProtocol {

		public const char TYPE_MODES= 'M';
		public const char TYPE_MODE_RAW= 'R';
		public const char TYPE_MODE_MERGE= 'M';
		public const char TYPE_MODE_DISTINCT= 'D';
		public const char TYPE_MODE_COMMAND= 'C';

        public const char TYPE_MPN_PLATFORM= 'P';
        public const char TYPE_MPN_PLATFORM_APPLE= 'A';
        public const char TYPE_MPN_PLATFORM_GOOGLE= 'G';

        public const char SUBTYPE_METADATAPROVIDER_EXCEPTION= 'M';
		public const char SUBTYPE_ACCESS_EXCEPTION= 'A';
		public const char SUBTYPE_CREDITS_EXCEPTION= 'C';
		public const char SUBTYPE_CONFLICTING_SESSION_EXCEPTION= 'X';
		public const char SUBTYPE_ITEMS_EXCEPTION= 'I';
		public const char SUBTYPE_SCHEMA_EXCEPTION= 'S';
		public const char SUBTYPE_NOTIFICATION_EXCEPTION= 'N';

		public const string METHOD_METADATA_INIT= "MPI";
		public const string METHOD_GET_ITEM_DATA= "GIT";
		public const string METHOD_NOTIFY_USER= "NUS";
		public const string METHOD_NOTIFY_USER_AUTH= "NUA";
		public const string METHOD_GET_SCHEMA= "GSC";
		public const string METHOD_GET_ITEMS= "GIS";
		public const string METHOD_GET_USER_ITEM_DATA= "GUI";
		public const string METHOD_NOTIFY_USER_MESSAGE= "NUM";
		public const string METHOD_NOTIFY_NEW_SESSION= "NNS";
		public const string METHOD_NOTIFY_SESSION_CLOSE= "NSC";
		public const string METHOD_NOTIFY_NEW_TABLES= "NNT";
		public const string METHOD_NOTIFY_TABLES_CLOSE= "NTC";
        public const string METHOD_NOTIFY_MPN_DEVICE_ACCESS= "MDA";
        public const string METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION= "MSA";
        public const string METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE= "MDC";

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
						headerName = DecodeString(val);
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_METADATA_INIT + " request");
				}

				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0])
				{

					case TYPE_STRING:
						string val = tokenizer.NextToken();
						headerValue = DecodeString(val);
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_METADATA_INIT + " request");
				}

				parameters[headerName] = headerValue;
			}

			return parameters;
		}

		public static string WriteInit(IDictionary arguments) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_METADATA_INIT);

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
                    sb.Append(EncodeString((string) iter.Entry.Key));
                    sb.Append(SEP);
                    sb.Append(TYPE_STRING);
                    sb.Append(SEP);
                    sb.Append(EncodeString((string) iter.Entry.Value));
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

			sb.Append(METHOD_METADATA_INIT);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is MetadataProviderException) sb.Append(SUBTYPE_METADATAPROVIDER_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));

			return sb.ToString();
		}

		///////////////////////////////////////////////////////////////////////////
		// GET ITEM DATA

		public static string [] ReadGetItemData(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			IList itemList= new ArrayList();

			while (tokenizer.HasMoreTokens()) {
				String typ= tokenizer.NextToken();

				switch (typ.ToCharArray()[0]) {

					case TYPE_STRING:
						string item= tokenizer.NextToken();
						itemList.Add(DecodeString(item));
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_ITEM_DATA + " request");
				}

			}

			string [] items= new string [itemList.Count];
			for (int i= 0; i < itemList.Count; i++) items[i]= (string) itemList[i];
			return items;
		}

		public static string WriteGetItemData(ItemData [] itemDatas) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_GET_ITEM_DATA);

			foreach (ItemData itemData in itemDatas) {
				sb.Append(SEP);
				sb.Append(TYPE_INT);
				sb.Append(SEP);
				sb.Append(itemData.DistinctSnapshotLength);
				sb.Append(SEP);
				sb.Append(TYPE_DOUBLE);
				sb.Append(SEP);
				sb.Append(EncodeDouble(itemData.MinSourceFrequency));
				sb.Append(SEP);
				sb.Append(TYPE_MODES);
				sb.Append(SEP);
				sb.Append(EncodeModes(itemData.AllowedModes));
			}

			return sb.ToString();
		}

		public static string WriteGetItemData(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_GET_ITEM_DATA);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// NOTIFY USER

		public static NotifyUserData ReadNotifyUser(string request, String methodVersion) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			NotifyUserData data= new NotifyUserData();

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
                throw new RemotingException("Token not found while parsing a " + methodVersion + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.User= DecodeString(tokenizer.NextToken());
					break;

				default:
                    throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + methodVersion + " request");
			}

			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
                throw new RemotingException("Token not found while parsing a " + methodVersion + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.Password= DecodeString(tokenizer.NextToken());
					break;

				default:
                    throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + methodVersion + " request");
			}

            if (methodVersion == METHOD_NOTIFY_USER_AUTH) {
			    try {
				    typ= tokenizer.NextToken();
			    } catch (IndexOutOfRangeException) {
                    throw new RemotingException("Token not found while parsing a " + methodVersion + " request");
			    }
			    switch (typ.ToCharArray()[0]) {

				    case TYPE_STRING:
					    data.clientPrincipal = DecodeString(tokenizer.NextToken());
					    break;

				    default:
                        throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + methodVersion + " request");
			    }
            }

			while (tokenizer.HasMoreTokens()) {
				string headerName;
				string headerValue;

				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0])
				{

					case TYPE_STRING:
						string val = tokenizer.NextToken();
						headerName = DecodeString(val);
						break;

					default:
                        throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + methodVersion + " request");
				}

				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0])
				{

					case TYPE_STRING:
						string val = tokenizer.NextToken();
						headerValue = DecodeString(val);
						break;

					default:
                        throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + methodVersion + " request");
				}

				data.httpHeaders[headerName] = headerValue;
			}

			return data;
		}

		public static string WriteNotifyUser(UserData userData, String methodVersion) {
			StringBuilder sb= new StringBuilder();

            sb.Append(methodVersion);
			sb.Append(SEP);
			sb.Append(TYPE_DOUBLE);
			sb.Append(SEP);
			sb.Append(EncodeDouble(userData.AllowedMaxBandwidth));
			sb.Append(SEP);
			sb.Append(TYPE_BOOLEAN);
			sb.Append(SEP);
			sb.Append(userData.WantsTablesNotification ? VALUE_TRUE : VALUE_FALSE);

			return sb.ToString();
		}

		public static string WriteNotifyUser(Exception exception, String methodVersion) {
			StringBuilder sb= new StringBuilder();

            sb.Append(methodVersion);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is AccessException) sb.Append(SUBTYPE_ACCESS_EXCEPTION);
			if (exception is CreditsException) sb.Append(SUBTYPE_CREDITS_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));
			if (exception is CreditsException) {
				sb.Append(SEP);
				sb.Append(((CreditsException) exception).ClientErrorCode);
				sb.Append(SEP);
				sb.Append(EncodeString(((CreditsException) exception).ClientErrorMsg));
			}

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// GET SCHEMA

		public static GetSchemaData ReadGetSchema(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			GetSchemaData data= new GetSchemaData();

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_GET_SCHEMA + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.User= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_SCHEMA + " request");
			}

			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_GET_SCHEMA + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.Group= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_SCHEMA + " request");
			}

			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_GET_SCHEMA + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.Schema= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_SCHEMA + " request");
			}

            try
            {
                typ = tokenizer.NextToken();
            }
            catch (IndexOutOfRangeException)
            {
                throw new RemotingException("Token not found while parsing a " + METHOD_GET_SCHEMA + " request");
            }
            switch (typ.ToCharArray()[0])
            {

                case TYPE_STRING:
                    data.Session = DecodeString(tokenizer.NextToken());
                    break;

                default:
                    throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_SCHEMA + " request");
            }

            return data;
		}

		public static string WriteGetSchema(string [] fields) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_GET_SCHEMA);

			foreach (string field in fields) {
				sb.Append(SEP);
				sb.Append(TYPE_STRING);
				sb.Append(SEP);
				sb.Append(EncodeString(field));
			}

			return sb.ToString();
		}

		public static string WriteGetSchema(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_GET_SCHEMA);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is ItemsException) sb.Append(SUBTYPE_ITEMS_EXCEPTION);
			if (exception is SchemaException) sb.Append(SUBTYPE_SCHEMA_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// GET ITEMS

		public static GetItemsData ReadGetItems(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			GetItemsData data= new GetItemsData();

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_GET_ITEMS + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.User= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_ITEMS + " request");
			}

			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_GET_ITEMS + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.Group= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_ITEMS + " request");
			}

            try
            {
                typ = tokenizer.NextToken();
            }
            catch (IndexOutOfRangeException)
            {
                throw new RemotingException("Token not found while parsing a " + METHOD_GET_ITEMS + " request");
            }
            switch (typ.ToCharArray()[0])
            {

                case TYPE_STRING:
                    data.Session = DecodeString(tokenizer.NextToken());
                    break;

                default:
                    throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_ITEMS + " request");
            }

            return data;
		}

		public static string WriteGetItems(string [] items) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_GET_ITEMS);

			foreach (string item in items) {
				sb.Append(SEP);
				sb.Append(TYPE_STRING);
				sb.Append(SEP);
				sb.Append(EncodeString(item));
			}

			return sb.ToString();
		}

		public static string WriteGetItems(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_GET_ITEMS);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is ItemsException) sb.Append(SUBTYPE_ITEMS_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// GET USER ITEM DATA

		public static GetUserItemData ReadGetUserItemData(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			GetUserItemData data= new GetUserItemData();

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_GET_USER_ITEM_DATA + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.User= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_USER_ITEM_DATA + " request");
			}

			IList itemList= new ArrayList();

			while (tokenizer.HasMoreTokens()) {
				typ= tokenizer.NextToken();

				switch (typ.ToCharArray()[0]) {

					case TYPE_STRING:
						string item= tokenizer.NextToken();
						itemList.Add(DecodeString(item));
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_GET_USER_ITEM_DATA + " request");
				}

			}

			string [] items= new string [itemList.Count];
			for (int i= 0; i < itemList.Count; i++) items[i]= (string) itemList[i];
			data.Items= items;

			return data;
		}

		public static string WriteGetUserItemData(UserItemData [] userItemDatas) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_GET_USER_ITEM_DATA);

			foreach (UserItemData userItemData in userItemDatas) {
				sb.Append(SEP);
				sb.Append(TYPE_INT);
				sb.Append(SEP);
				sb.Append(userItemData.AllowedBufferSize);
				sb.Append(SEP);
				sb.Append(TYPE_DOUBLE);
				sb.Append(SEP);
				sb.Append(EncodeDouble(userItemData.AllowedMaxItemFrequency));
				sb.Append(SEP);
				sb.Append(TYPE_MODES);
				sb.Append(SEP);
				sb.Append(EncodeModes(userItemData.AllowedModes));
			}

			return sb.ToString();
		}

		public static string WriteGetUserItemData(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_GET_USER_ITEM_DATA);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// NOTIFY USER MESSAGE

		public static NotifyUserMessageData ReadNotifyUserMessage(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			NotifyUserMessageData data= new NotifyUserMessageData();

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_USER_MESSAGE + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.User= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_USER_MESSAGE + " request");
			}

			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_USER_MESSAGE + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.Session= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_USER_MESSAGE + " request");
			}

			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_USER_MESSAGE + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.Message= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_USER_MESSAGE + " request");
			}

			return data;
		}

		public static string WriteNotifyUserMessage() {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_USER_MESSAGE);
			sb.Append(SEP);
			sb.Append(TYPE_VOID);

			return sb.ToString();
		}

		public static string WriteNotifyUserMessage(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_USER_MESSAGE);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is NotificationException) sb.Append(SUBTYPE_NOTIFICATION_EXCEPTION);
			if (exception is CreditsException) sb.Append(SUBTYPE_CREDITS_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));
			if (exception is CreditsException) {
				sb.Append(SEP);
				sb.Append(((CreditsException) exception).ClientErrorCode);
				sb.Append(SEP);
				sb.Append(EncodeString(((CreditsException) exception).ClientErrorMsg));
			}

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// NOTIFY NEW SESSION

		public static NotifyNewSessionData ReadNotifyNewSession(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			NotifyNewSessionData data= new NotifyNewSessionData();

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_NEW_SESSION + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.User= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_SESSION + " request");
			}

			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_NEW_SESSION + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.Session= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_SESSION + " request");
			}

            while (tokenizer.HasMoreTokens())
            {
                string contextInfoName;
                string contextInfoValue;

                typ = tokenizer.NextToken();

                switch (typ.ToCharArray()[0])
                {

                    case TYPE_STRING:
                        string val = tokenizer.NextToken();
                        contextInfoName = DecodeString(val);
                        break;

                    default:
                        throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_SESSION + " request");
                }

                typ = tokenizer.NextToken();

                switch (typ.ToCharArray()[0])
                {

                    case TYPE_STRING:
                        string val = tokenizer.NextToken();
                        contextInfoValue = DecodeString(val);
                        break;

                    default:
                        throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_SESSION + " request");
                }

                data.clientContext[contextInfoName] = contextInfoValue;
            }

            return data;
		}

		public static string WriteNotifyNewSession() {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_NEW_SESSION);
			sb.Append(SEP);
			sb.Append(TYPE_VOID);

			return sb.ToString();
		}

		public static string WriteNotifyNewSession(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_NEW_SESSION);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is NotificationException) sb.Append(SUBTYPE_NOTIFICATION_EXCEPTION);
			if (exception is CreditsException) {
				if (exception is ConflictingSessionException) sb.Append(SUBTYPE_CONFLICTING_SESSION_EXCEPTION);
				else sb.Append(SUBTYPE_CREDITS_EXCEPTION);
			}
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));
			if (exception is CreditsException) {
				sb.Append(SEP);
				sb.Append(((CreditsException) exception).ClientErrorCode);
				sb.Append(SEP);
				sb.Append(EncodeString(((CreditsException) exception).ClientErrorMsg));
				if (exception is ConflictingSessionException) {
					sb.Append(SEP);
					sb.Append(EncodeString(((ConflictingSessionException) exception).ConflictingSessionID));
				}
			}

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// NOTIFY SESSION CLOSE

		public static string ReadNotifySessionClose(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_SESSION_CLOSE + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					string session= tokenizer.NextToken();
					return DecodeString(session);

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_SESSION_CLOSE + " request");
			}
		}

		public static string WriteNotifySessionClose() {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_SESSION_CLOSE);
			sb.Append(SEP);
			sb.Append(TYPE_VOID);

			return sb.ToString();
		}

		public static string WriteNotifySessionClose(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_SESSION_CLOSE);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is NotificationException) sb.Append(SUBTYPE_NOTIFICATION_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// NOTIFY NEW TABLES

		public static NotifyNewTablesData ReadNotifyNewTables(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			NotifyNewTablesData data= new NotifyNewTablesData();

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.User= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
			}

			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.Session= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
			}

			IList tableList= new ArrayList();

			while (tokenizer.HasMoreTokens()) {
				int winIndex= -1;
				Mode mode= null;
				string id= null;
				string schema= null;
				int min= -1;
				int max= -1;
				string selector= null;

				typ= tokenizer.NextToken();

				switch (typ.ToCharArray()[0]) {

					case TYPE_INT:
						winIndex= Int32.Parse(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}

				try {
					typ= tokenizer.NextToken();
				} catch (IndexOutOfRangeException) {
					throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}
				switch (typ.ToCharArray()[0]) {

					case TYPE_MODES:
						Mode [] modes= DecodeModes(tokenizer.NextToken());
						if (modes != null) mode= modes[0];
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}

				try {
					typ= tokenizer.NextToken();
				} catch (IndexOutOfRangeException) {
					throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}
				switch (typ.ToCharArray()[0]) {

					case TYPE_STRING:
						id= DecodeString(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}

				try {
					typ= tokenizer.NextToken();
				} catch (IndexOutOfRangeException) {
					throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}
				switch (typ.ToCharArray()[0]) {

					case TYPE_STRING:
						schema= DecodeString(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}

				if (! tokenizer.HasMoreTokens()) {
					break;
				}
				typ= tokenizer.NextToken();

				switch (typ.ToCharArray()[0]) {

					case TYPE_INT:
						min= Int32.Parse(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}

				if (! tokenizer.HasMoreTokens()) {
					break;
				}
				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0]) {

					case TYPE_INT:
						max= Int32.Parse(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}

				try {
					typ= tokenizer.NextToken();
				} catch (IndexOutOfRangeException) {
					throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}
				switch (typ.ToCharArray()[0]) {

					case TYPE_STRING:
						selector= DecodeString(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_NEW_TABLES + " request");
				}

				TableInfo table= new TableInfo(winIndex, mode, id, schema, min, max, selector);
				tableList.Add(table);

			}

			TableInfo [] tables= new TableInfo [(tableList.Count)];
			for (int i= 0; i < tableList.Count; i++) tables[i]= (TableInfo) tableList[i];
			data.Tables= tables;

			return data;
		}

		public static string WriteNotifyNewTables() {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_NEW_TABLES);
			sb.Append(SEP);
			sb.Append(TYPE_VOID);

			return sb.ToString();
		}

		public static string WriteNotifyNewTables(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_NEW_TABLES);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is NotificationException) sb.Append(SUBTYPE_NOTIFICATION_EXCEPTION);
			if (exception is CreditsException) sb.Append(SUBTYPE_CREDITS_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));
			if (exception is CreditsException) {
				sb.Append(SEP);
				sb.Append(((CreditsException) exception).ClientErrorCode);
				sb.Append(SEP);
				sb.Append(EncodeString(((CreditsException) exception).ClientErrorMsg));
			}

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// NOTIFY TABLES CLOSE

		public static NotifyTablesCloseData ReadNotifyTablesClose(string request) {
			StringTokenizer tokenizer= new StringTokenizer(request, "" + SEP);

			NotifyTablesCloseData data= new NotifyTablesCloseData();

			String typ= null;
			try {
				typ= tokenizer.NextToken();
			} catch (IndexOutOfRangeException) {
				throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
			}
			switch (typ.ToCharArray()[0]) {

				case TYPE_STRING:
					data.Session= DecodeString(tokenizer.NextToken());
					break;

				default:
					throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
			}

			IList tableList= new ArrayList();

			while (tokenizer.HasMoreTokens()) {
				int winIndex = -1;
				Mode mode= null;
				string id= null;
				string schema= null;
				int min= -1;
				int max= -1;
				string selector= null;

				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0]) {

					case TYPE_INT:
						winIndex= Int32.Parse(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}

				try {
					typ= tokenizer.NextToken();
				} catch (IndexOutOfRangeException) {
					throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}
				switch (typ.ToCharArray()[0]) {

					case TYPE_MODES:
						Mode [] modes= DecodeModes(tokenizer.NextToken());
						if (modes != null) mode= modes[0];
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}

				try {
					typ= tokenizer.NextToken();
				} catch (IndexOutOfRangeException) {
					throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}
				switch (typ.ToCharArray()[0]) {

					case TYPE_STRING:
						id= DecodeString(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}

				try {
					typ= tokenizer.NextToken();
				} catch (IndexOutOfRangeException) {
					throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}
				switch (typ.ToCharArray()[0]) {

					case TYPE_STRING:
						schema= DecodeString(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}

				if (! tokenizer.HasMoreTokens()) {
					break;
				}
				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0]) {

					case TYPE_INT:
						min= Int32.Parse(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}

				if (! tokenizer.HasMoreTokens()) {
					break;
				}
				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0]) {

					case TYPE_INT:
						max= Int32.Parse(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}

				try {
					typ= tokenizer.NextToken();
				} catch (IndexOutOfRangeException) {
					throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}
				switch (typ.ToCharArray()[0]) {

					case TYPE_STRING:
						selector= DecodeString(tokenizer.NextToken());
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_NOTIFY_TABLES_CLOSE + " request");
				}

				TableInfo table= new TableInfo(winIndex, mode, id, schema, min, max, selector);
				tableList.Add(table);

			}

			TableInfo [] tables= new TableInfo [(tableList.Count)];
			for (int i= 0; i < tableList.Count; i++) tables[i]= (TableInfo) tableList[i];
			data.Tables= tables;

			return data;
		}

		public static string WriteNotifyTablesClose() {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_TABLES_CLOSE);
			sb.Append(SEP);
			sb.Append(TYPE_VOID);

			return sb.ToString();
		}

		public static string WriteNotifyTablesClose(Exception exception) {
			StringBuilder sb= new StringBuilder();

			sb.Append(METHOD_NOTIFY_TABLES_CLOSE);
			sb.Append(SEP);
			sb.Append(TYPE_EXCEPTION);
			if (exception is NotificationException) sb.Append(SUBTYPE_NOTIFICATION_EXCEPTION);
			sb.Append(SEP);
			sb.Append(EncodeString(exception.Message));

			return sb.ToString();
		}

        // ////////////////////////////////////////////////////////////////////////
        // NOTIFY MPN DEVICE ACCESS

        public static NotifyMpnDeviceAccessData ReadNotifyMpnDeviceAccess(string request) {
            NotifyMpnDeviceAccessData data = new NotifyMpnDeviceAccessData();

            StringTokenizer tokenizer = new StringTokenizer(request, "" + SEP);
            try {

                // User
                string typ = tokenizer.NextToken();
                if (typ.ToCharArray()[0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_ACCESS + " request");

                data.User = DecodeString(tokenizer.NextToken());

                // Session ID
                typ = tokenizer.NextToken();
                if (typ.ToCharArray()[0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_ACCESS + " request");

                data.SessionID = DecodeString(tokenizer.NextToken());

                // Platform type
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_MPN_PLATFORM)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_ACCESS + " request");

                MpnPlatformType platformType = DecodeMpnPlatformType(tokenizer.NextToken());

                // Application ID
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_ACCESS + " request");

                string appID = DecodeString(tokenizer.NextToken());

                // Device token
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_ACCESS + " request");

                string deviceToken = DecodeString(tokenizer.NextToken());

                data.Device = new MpnDeviceInfo(platformType, appID, deviceToken);

            } catch (IndexOutOfRangeException) {
                throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_ACCESS + " request");
            }

            return data;
        }

        public static string WriteNotifyMpnDeviceAccess() {
            StringBuilder sb = new StringBuilder();

            sb.Append(METHOD_NOTIFY_MPN_DEVICE_ACCESS);
            sb.Append(SEP);
            sb.Append(TYPE_VOID);

            return sb.ToString();
        }

        public static string WriteNotifyMpnDeviceAccess(Exception exception) {
            StringBuilder sb = new StringBuilder();

            sb.Append(METHOD_NOTIFY_MPN_DEVICE_ACCESS);
            sb.Append(SEP);
            sb.Append(TYPE_EXCEPTION);
            if (exception is NotificationException) sb.Append(SUBTYPE_NOTIFICATION_EXCEPTION);
            if (exception is CreditsException) sb.Append(SUBTYPE_CREDITS_EXCEPTION);
            sb.Append(SEP);
            sb.Append(EncodeString(exception.Message));
            if (exception is CreditsException) {
                sb.Append(SEP);
                sb.Append(((CreditsException) exception).ClientErrorCode);
                sb.Append(SEP);
                sb.Append(EncodeString(((CreditsException) exception).ClientErrorMsg));
            }

            return sb.ToString();
        }

        // ////////////////////////////////////////////////////////////////////////
        // NOTIFY MPN SUBSCRIPTION ACTIVATION

        public static NotifyMpnSubscriptionActivationData ReadNotifyMpnSubscriptionActivation(string request) {
            NotifyMpnSubscriptionActivationData data = new NotifyMpnSubscriptionActivationData();

            StringTokenizer tokenizer = new StringTokenizer(request, "" + SEP);
            try {

                // User
                string typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                data.User = DecodeString(tokenizer.NextToken());

                // Session ID
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                data.SessionID = DecodeString(tokenizer.NextToken());

                // Table info: win index
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_INT)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                int winIndex = Int32.Parse(tokenizer.NextToken());

                // Table info: mode
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_MODES)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                Mode [] modes = DecodeModes(tokenizer.NextToken());
                Mode mode = (modes != null) ? modes [0] : null;

                // Table info: ID
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                string id = DecodeString(tokenizer.NextToken());

                // Table info: schema
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                string schema = DecodeString(tokenizer.NextToken());

                // Table info: min
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_INT)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                int min = Int32.Parse(tokenizer.NextToken());

                // Table info: max
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_INT)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                int max = Int32.Parse(tokenizer.NextToken());

                TableInfo table = new TableInfo(winIndex, mode, id, schema, min, max, null);
                data.Table = table;

                // Platform type
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_MPN_PLATFORM)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                MpnPlatformType platformType = DecodeMpnPlatformType(tokenizer.NextToken());

                // Application ID
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                string appID = DecodeString(tokenizer.NextToken());

                // Device token
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                string deviceToken = DecodeString(tokenizer.NextToken());

                // Trigger
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                string trigger = DecodeString(tokenizer.NextToken());

                // Notification Format
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");

                string notificationFormat = DecodeString(tokenizer.NextToken());

                MpnDeviceInfo deviceInfo= new MpnDeviceInfo(platformType, appID, deviceToken);
                MpnSubscriptionInfo subscription = new MpnSubscriptionInfo(deviceInfo, notificationFormat, trigger);

                data.MpnSubscription = subscription;

            } catch (IndexOutOfRangeException) {
                throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION + " request");
            }

            return data;
        }

        public static string WriteNotifyMpnSubscriptionActivation() {
            StringBuilder sb = new StringBuilder();

            sb.Append(METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION);
            sb.Append(SEP);
            sb.Append(TYPE_VOID);

            return sb.ToString();
        }

        public static string WriteNotifyMpnSubscriptionActivation(Exception exception) {
            StringBuilder sb = new StringBuilder();

            sb.Append(METHOD_NOTIFY_MPN_SUBSCRIPTION_ACTIVATION);
            sb.Append(SEP);
            sb.Append(TYPE_EXCEPTION);
            if (exception is NotificationException) sb.Append(SUBTYPE_NOTIFICATION_EXCEPTION);
            if (exception is CreditsException) sb.Append(SUBTYPE_CREDITS_EXCEPTION);
            sb.Append(SEP);
            sb.Append(EncodeString(exception.Message));
            if (exception is CreditsException) {
                sb.Append(SEP);
                sb.Append(((CreditsException) exception).ClientErrorCode);
                sb.Append(SEP);
                sb.Append(EncodeString(((CreditsException) exception).ClientErrorMsg));
            }

            return sb.ToString();
        }

        // ////////////////////////////////////////////////////////////////////////
        // NOTIFY MPN DEVICE TOKEN CHANGE

        public static NotifyMpnDeviceTokenChangeData ReadNotifyMpnDeviceTokenChange(string request) {
            NotifyMpnDeviceTokenChangeData data = new NotifyMpnDeviceTokenChangeData();

            StringTokenizer tokenizer = new StringTokenizer(request, "" + SEP);
            try {

                // User
                string typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE + " request");

                data.User = DecodeString(tokenizer.NextToken());

                // Session ID
                typ = tokenizer.NextToken();
                if (typ.ToCharArray()[0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE + " request");

                data.SessionID = DecodeString(tokenizer.NextToken());

                // Platform type
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_MPN_PLATFORM)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE + " request");

                MpnPlatformType platformType = DecodeMpnPlatformType(tokenizer.NextToken());

                // Application ID
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE + " request");

                string appID = DecodeString(tokenizer.NextToken());

                // Device token
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE + " request");

                string deviceToken = DecodeString(tokenizer.NextToken());

                data.Device = new MpnDeviceInfo(platformType, appID, deviceToken);

                // New device token
                typ = tokenizer.NextToken();
                if (typ.ToCharArray() [0] != TYPE_STRING)
                    throw new RemotingException("Unexpected type '" + typ + "' found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE + " request");

                string newDeviceToken = DecodeString(tokenizer.NextToken());

                data.NewDeviceToken = newDeviceToken;

            } catch (IndexOutOfRangeException) {
                throw new RemotingException("Token not found while parsing a " + METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE + " request");
            }

            return data;
        }

        public static string WriteNotifyMpnDeviceTokenChange() {
            StringBuilder sb = new StringBuilder();

            sb.Append(METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE);
            sb.Append(SEP);
            sb.Append(TYPE_VOID);

            return sb.ToString();
        }

        public static string WriteNotifyMpnDeviceTokenChange(Exception exception) {
            StringBuilder sb = new StringBuilder();

            sb.Append(METHOD_NOTIFY_MPN_DEVICE_TOKEN_CHANGE);
            sb.Append(SEP);
            sb.Append(TYPE_EXCEPTION);
            if (exception is NotificationException) sb.Append(SUBTYPE_NOTIFICATION_EXCEPTION);
            if (exception is CreditsException) sb.Append(SUBTYPE_CREDITS_EXCEPTION);
            sb.Append(SEP);
            sb.Append(EncodeString(exception.Message));
            if (exception is CreditsException) {
                sb.Append(SEP);
                sb.Append(((CreditsException) exception).ClientErrorCode);
                sb.Append(SEP);
                sb.Append(EncodeString(((CreditsException) exception).ClientErrorMsg));
            }

            return sb.ToString();
        }

        // ////////////////////////////////////////////////////////////////////////
		// Internal methods

		protected static string EncodeModes(Mode [] modes) {
			if (modes == null) return VALUE_NULL;
			if (modes.Length == 0) return VALUE_EMPTY;

			StringBuilder encodedModes= new StringBuilder();

			for (int i= 0; i < modes.Length; i++) {
				if (modes[i].Equals(Mode.RAW)) encodedModes.Append(TYPE_MODE_RAW);
				else if (modes[i].Equals(Mode.MERGE)) encodedModes.Append(TYPE_MODE_MERGE);
				else if (modes[i].Equals(Mode.DISTINCT)) encodedModes.Append(TYPE_MODE_DISTINCT);
				else if (modes[i].Equals(Mode.COMMAND)) encodedModes.Append(TYPE_MODE_COMMAND);
				else throw new RemotingException("Unknown mode '" + modes[i].ToString() + "' found while encoding Mode array");
			}

			return encodedModes.ToString();
		}

		protected static Mode [] DecodeModes(string str) {
			if (str.Equals(VALUE_NULL)) return null;
			if (str.Equals(VALUE_EMPTY)) return new Mode [0];

			Mode [] modes= new Mode [str.Length];

			char [] encodedModes= str.ToCharArray();
			for (int i= 0; i < str.Length; i++) {
				char encodedMode= encodedModes[i];
				switch (encodedMode) {
					case TYPE_MODE_RAW: modes[i]= Mode.RAW; break;
					case TYPE_MODE_MERGE: modes[i]= Mode.MERGE; break;
					case TYPE_MODE_DISTINCT: modes[i]= Mode.DISTINCT; break;
					case TYPE_MODE_COMMAND: modes[i]= Mode.COMMAND; break;
					default: throw new RemotingException("Unknown mode '" + encodedMode + "' found while decoding Mode array");
				}
			}

			return modes;
		}

		protected static string EncodeDouble(double val) {
			return val.ToString(CultureInfo.InvariantCulture);
		}

		protected static double DecodeDouble(string str) {
			return Double.Parse(str, CultureInfo.InvariantCulture);
		}

	    protected static char EncodeMpnPlatformType(MpnPlatformType platformType) {
            if (platformType == null) return VALUE_NULL.ToCharArray()[0];

            if (platformType.Equals(MpnPlatformType.Apple.ToString())) return TYPE_MPN_PLATFORM_APPLE;
            else if (platformType.Equals(MpnPlatformType.Google.ToString())) return TYPE_MPN_PLATFORM_GOOGLE;
            else throw new RemotingException("Unknown platform type '" + platformType.ToString() + "'");
	    }

	    protected static MpnPlatformType DecodeMpnPlatformType(string str) {
            if (str.Equals(VALUE_NULL)) return null;

            char encodedPlatformType = str.ToCharArray()[0];
            switch (encodedPlatformType) {
                case TYPE_MPN_PLATFORM_APPLE: return MpnPlatformType.Apple;
                case TYPE_MPN_PLATFORM_GOOGLE: return MpnPlatformType.Google;
                default: throw new RemotingException("Unknown platform type '" + encodedPlatformType + "'");
            }
	    }
	}

	// ////////////////////////////////////////////////////////////////////////
	// Support classes

	internal class NotifyUserData {
		public string User;
		public string Password;
		public IDictionary httpHeaders = new Hashtable();
        public string clientPrincipal;
	}

	internal class GetSchemaData {
		public string User;
        public string Session;
        public string Group;
		public string Schema;
	}

	internal class GetItemsData {
		public string User;
        public string Session;
        public string Group;
	}

	internal class GetUserItemData {
		public string User;
		public string [] Items;
	}

	internal class NotifyUserMessageData {
		public string User;
		public string Session;
		public string Message;
	}

	internal class NotifyNewSessionData {
		public string User;
		public string Session;
        public IDictionary clientContext = new Hashtable();
    }

	internal class NotifyNewTablesData {
		public string User;
		public string Session;
		public TableInfo [] Tables;
	}

	internal class NotifyTablesCloseData {
		public string Session;
		public TableInfo [] Tables;
	}

    internal class NotifyMpnDeviceAccessData {
        public string User;
        public string SessionID;
        public MpnDeviceInfo Device;
    }

    internal class NotifyMpnSubscriptionActivationData {
        public string User;
        public string SessionID;
        public TableInfo Table;
        public MpnSubscriptionInfo MpnSubscription;
    }

    internal class NotifyMpnDeviceTokenChangeData {
        public string User;
        public string SessionID;
        public MpnDeviceInfo Device;
        public string NewDeviceToken;
    }

	internal class ItemData {
		public int DistinctSnapshotLength;
		public double MinSourceFrequency;
		public Mode [] AllowedModes;
	}

	internal class UserData {
		public double AllowedMaxBandwidth;
		public bool WantsTablesNotification;
	}

	internal class UserItemData {
		public int AllowedBufferSize;
		public double AllowedMaxItemFrequency;
		public Mode [] AllowedModes;
	}

}
