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
