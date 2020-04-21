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
 * 
 * Base64 class taken from: http://www.thecodeproject.com/csharp/Base64EncDec.asp
 */

using System;
using System.Collections;

namespace Lightstreamer.DotNet.Utils {

	internal class Base64Encoder {
		byte[] source;
		int length,length2;
		int blockCount;
		int paddingCount;
		public Base64Encoder(byte[] input) {
			source=input;
			length=input.Length;
			if((length % 3)==0) {
				paddingCount=0;
				blockCount=length/3;
			}
			else {
				paddingCount=3-(length % 3);//need to add padding
				blockCount=(length+paddingCount) / 3;
			}
			length2=length+paddingCount;//or blockCount *3
		}

		public char[] GetEncoded() {
			byte[] source2;
			source2=new byte[length2];
			//copy data over insert padding
			for (int x=0; x<length2;x++) {
				if (x<length) {
					source2[x]=source[x];
				}
				else {
					source2[x]=0;
				}
			}
	  
			byte b1, b2, b3;
			byte temp, temp1, temp2, temp3, temp4;
			byte[] buffer=new byte[blockCount*4];
			char[] result=new char[blockCount*4];
			for (int x=0;x<blockCount;x++) {
				b1=source2[x*3];
				b2=source2[x*3+1];
				b3=source2[x*3+2];

				temp1=(byte)((b1 & 252)>>2);//first

				temp=(byte)((b1 & 3)<<4);
				temp2=(byte)((b2 & 240)>>4);
				temp2+=temp; //second

				temp=(byte)((b2 & 15)<<2);
				temp3=(byte)((b3 & 192)>>6);
				temp3+=temp; //third

				temp4=(byte)(b3 & 63); //fourth

				buffer[x*4]=temp1;
				buffer[x*4+1]=temp2;
				buffer[x*4+2]=temp3;
				buffer[x*4+3]=temp4;

			}

			for (int x=0; x<blockCount*4;x++) {
				result[x]=sixbit2char(buffer[x]);
			}

			//covert last "A"s to "=", based on paddingCount
			switch (paddingCount) {
				case 0:break;
				case 1:result[blockCount*4-1]='=';break;
				case 2:result[blockCount*4-1]='=';
					result[blockCount*4-2]='=';
					break;
				default:break;
			}
			return result;
		}

		private char sixbit2char(byte b) {
			char[] lookupTable=new char[64] {
			 'A','B','C','D','E','F','G','H','I','J','K','L','M',
			  'N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
			  'a','b','c','d','e','f','g','h','i','j','k','l','m',
			  'n','o','p','q','r','s','t','u','v','w','x','y','z',
			  '0','1','2','3','4','5','6','7','8','9','+','/'};

			if((b>=0) &&(b<=63)) {
				return lookupTable[(int)b];
			}
			else {
				//should not happen;
				return ' ';
			}
		}
	}

	internal class Base64Decoder {
		char[] source;
		int length, length2, length3;
		int blockCount;
		int paddingCount;
		public Base64Decoder(char[] input) {
			int temp=0;
			source=input;
			length=input.Length;

			//find how many padding are there
			for (int x=0;x<2;x++) {
				if(input[length-x-1]=='=')
					temp++;
			}
			paddingCount=temp;
			//calculate the blockCount;
			//assuming all whitespace and carriage returns/newline were removed.
			blockCount=length/4;
			length2=blockCount*3;
		}

		public byte[] GetDecoded() {
			byte[] buffer=new byte[length];//first conversion result
			byte[] buffer2=new byte[length2];//decoded array with padding

			for(int x=0;x<length;x++) {
				buffer[x]=char2sixbit(source[x]);
			}

			byte b, b1,b2,b3;
			byte temp1, temp2, temp3, temp4;

			for(int x=0;x<blockCount;x++) {
				temp1=buffer[x*4];
				temp2=buffer[x*4+1];
				temp3=buffer[x*4+2];
				temp4=buffer[x*4+3];		

				b=(byte)(temp1<<2);
				b1=(byte)((temp2 & 48)>>4);
				b1+=b;

				b=(byte)((temp2 & 15)<<4);
				b2=(byte)((temp3 & 60)>>2);
				b2+=b;

				b=(byte)((temp3 & 3)<<6);
				b3=temp4;
				b3+=b;

				buffer2[x*3]=b1;
				buffer2[x*3+1]=b2;
				buffer2[x*3+2]=b3;
			}
			//remove paddings
			length3=length2-paddingCount;
			byte[] result=new byte[length3];

			for(int x=0;x<length3;x++) {
				result[x]=buffer2[x];
			}

			return result;
		}

		private byte char2sixbit(char c) {
			char[] lookupTable=new char[64] {  

			  'A','B','C','D','E','F','G','H','I','J','K','L','M','N',
			  'O','P','Q','R','S','T','U','V','W','X','Y', 'Z',
			  'a','b','c','d','e','f','g','h','i','j','k','l','m','n',
			  'o','p','q','r','s','t','u','v','w','x','y','z',
			  '0','1','2','3','4','5','6','7','8','9','+','/'};
			if(c=='=')
				return 0;
			else {
				for (int x=0;x<64;x++) {
					if (lookupTable[x]==c)
						return (byte)x;
				}
				//should not reach here
				return 0;
			}

		}
	}

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
