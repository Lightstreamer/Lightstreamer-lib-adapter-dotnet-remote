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
using System.Collections;
using System.Web;
using System.Text;

using Lightstreamer.Interfaces.Data;
using Lightstreamer.DotNet.Utils;

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
		public const char TYPE_BYTES= 'Y';
		public const char TYPE_BOOLEAN= 'B';
		public const char TYPE_INT= 'I';
		public const char TYPE_DOUBLE= 'D';
		public const char TYPE_EXCEPTION= 'E';

        public const string METHOD_KEEPALIVE = "KEEPALIVE";
        public const string METHOD_REMOTE_CREDENTIALS = "RAC";

        public const string AUTH_REQUEST_ID = "1";

		protected static string EncodeString(string str) {
			if (str == null) return VALUE_NULL;
			if (str.Length == 0) return VALUE_EMPTY;

			try {
				return HttpUtility.UrlEncode(str);
			
			} catch (Exception e) {
				throw new RemotingException("Unknown error while url-encoding string", e);
			}
		}

		protected static string DecodeString(string str) {
			if (str.Equals(VALUE_NULL)) return null;
			if (str.Equals(VALUE_EMPTY)) return "";

			try {
				return HttpUtility.UrlDecode(str);
			
			} catch (Exception e) {
				throw new RemotingException("Unknown error while url-decoding string", e);
			}
		}

		protected static string EncodeBytes(byte [] bytes) {
			if (bytes == null) return VALUE_NULL;
			if (bytes.Length == 0) return VALUE_EMPTY;

			try {
				Base64Encoder encoder= new Base64Encoder(bytes);
				return new string(encoder.GetEncoded());
			
			} catch (Exception e) {
				throw new RemotingException("Unknown error while base64-encoding bytes", e);
			}
		}

		protected static byte [] DecodeBytes(string str) {
			if (str.Equals(VALUE_NULL)) return null;
			if (str.Equals(VALUE_EMPTY)) return new byte [0];

			try {
				Base64Decoder decoder= new Base64Decoder(str.ToCharArray());
				return decoder.GetDecoded();
			
			} catch (Exception e) {
				throw new RemotingException("Unknown error while base64-decoding bytes", e);
			}
		}
	}

}