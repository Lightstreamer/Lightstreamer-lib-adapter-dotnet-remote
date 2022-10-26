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

namespace Lightstreamer.DotNet.Utils {

	internal class StringTokenizer {

		private string [] _tokens;
		private int _pos;
	
		public StringTokenizer(string str, string separators) {
			IList tokens= new ArrayList(str.Split(separators.ToCharArray()));
			
			int pos= 0;
			while (pos < tokens.Count) {
				string token= (string) tokens[pos];
				if ((token == null) || (token.Length == 0)) tokens.RemoveAt(pos);
				else pos++;
			}

			_tokens= new string [tokens.Count];
			for (int i= 0; i < tokens.Count; i++) _tokens[i]= (string) tokens[i];

			_pos= -1;
		}
		
		public string NextToken() {
			_pos++;
			if (_pos >= _tokens.Length) throw new IndexOutOfRangeException("" + _pos + " >= " + _tokens.Length);
		
			return _tokens[_pos];
		}
		
		public bool HasMoreTokens() {
			return ((_pos +1) < _tokens.Length);
		}

		public int CountTokens() {
			return _tokens.Length;
		}
	}

}
