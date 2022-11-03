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
using System.Web;
using System.Text;

using Lightstreamer.Interfaces.Data;
using Lightstreamer.DotNet.Utils;
using System.Runtime.InteropServices;

namespace Lightstreamer.DotNet.Server {

	internal class RemotingException : ApplicationException {
	
		public RemotingException(string msg) : base(msg) {}
		public RemotingException(string msg, Exception e) : base(msg, e) {}
	}
	
	internal abstract class RemotingProtocol {

		public const char SEP= '|';
		
		public const string VALUE_NULL= "#";
		public const string VALUE_EMPTY= "$";
		public const string VALUE_TRUE= "1";
		public const string VALUE_FALSE= "0";
		
		public const char TYPE_VOID= 'V';
		public const char TYPE_STRING= 'S';
		public const char TYPE_BOOLEAN= 'B';
		public const char TYPE_INT= 'I';
		public const char TYPE_DOUBLE= 'D';
		public const char TYPE_EXCEPTION= 'E';

		protected static string EncodeStringOld(string str) {
			if (str == null) return VALUE_NULL;
			if (str.Length == 0) return VALUE_EMPTY;

			try {
				return HttpUtility.UrlEncode(str);
			
			} catch (Exception e) {
				throw new RemotingException("Unknown error while url-encoding string", e);
			}
		}

		protected static string DecodeStringOld(string str) {
			if (str.Equals(VALUE_NULL)) return null;
			if (str.Equals(VALUE_EMPTY)) return "";

			try {
				return HttpUtility.UrlDecode(str);
			
			} catch (Exception e) {
				throw new RemotingException("Unknown error while url-decoding string", e);
			}
		}

		protected static string EncodeString(String str) {
			// temporarily, we lean on the available urlEncode function to apply
			// the percent encoding, although in this way we lose all the benefits
			// of the new protocol, which requires percent encoding only on a few
			// characters; otherwise, we should implement the encoding manually
			// 
			// however, since the new encoding specifications exclude the
			// space-to-'+' rule, we need to correct only this case manually
			string oldEnc = DecodeStringOld(str);
			return oldEnc.Replace('+', ' '); // only allocates if '+' is found
									// (i.e. space is found in the original value)
		}

		protected static string DecodeString(String str) {
			// since the new encoding specifications suppress the '+' character
			// and since the UrlDecode algorithm supports unencoded characters,
			// we can use UrlDecode also with the new encoding;
			// we rely on the Proxy Adapter to obey the protocol, so we don't check
			// that indeed str doesn't contain the '+' character
			return DecodeStringOld(str);
		}

		// private static Encoding isoLatin = Encoding.Latin1; // since .NET 5
		private static Encoding isoLatin;
		static RemotingProtocol() {
			try {
				isoLatin = Encoding.GetEncoding("iso-8859-1");
			} catch (Exception e) {
				isoLatin = null;
			}
		}

		protected static string EncodeBytesAsString(byte[] bytes) {
	        if (bytes == null) return VALUE_NULL;
		    if (bytes.Length == 0) return VALUE_EMPTY;

			if (isoLatin == null) {
                throw new RemotingException("Missing support for iso-latin conversion");
			}
			char[] latinChars = new char[isoLatin.GetCharCount(bytes, 0, bytes.Length)];
			isoLatin.GetChars(bytes, 0, bytes.Length, latinChars, 0);
			string equivalentStr = new string(latinChars);

			return EncodeString(equivalentStr);
		}
	}

	internal class BaseProtocol : RemotingProtocol {

		public const string METHOD_KEEPALIVE = "KEEPALIVE";
		public const string METHOD_REMOTE_CREDENTIALS = "RAC";
		public const string METHOD_CLOSE= "CLOSE";

	    public const string KEY_CLOSE_REASON = "reason";

		public const string AUTH_REQUEST_ID = "1";
		public const string CLOSE_REQUEST_ID = "0";

		// ////////////////////////////////////////////////////////////////////////
		// REMOTE CREDENTIALS

		public static string WriteRemoteCredentials(IDictionary arguments)
		{
			// protocol version 1.8.2 and above
			StringBuilder sb = new StringBuilder();

			sb.Append(METHOD_REMOTE_CREDENTIALS);

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

			return sb.ToString();
		}

		// ////////////////////////////////////////////////////////////////////////
		// CLOSE

		public static IDictionary ReadClose(string request)
		{
			StringTokenizer tokenizer = new StringTokenizer(request, "" + SEP);

			IDictionary parameters = new Hashtable();

			String typ = null;
			while (tokenizer.HasMoreTokens())
			{
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
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_CLOSE + " request");
				}

				typ = tokenizer.NextToken();

				switch (typ.ToCharArray()[0])
				{

					case TYPE_STRING:
						string val = tokenizer.NextToken();
						headerValue = DecodeStringOld(val);
						break;

					default:
						throw new RemotingException("Unknown type '" + typ + "' found while parsing a " + METHOD_CLOSE + " request");
				}

				parameters[headerName] = headerValue;
			}

			return parameters;
		}

	}

}