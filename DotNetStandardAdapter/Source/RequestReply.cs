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
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using Lightstreamer.DotNet.Server.Log;

namespace Lightstreamer.DotNet.Server.RequestReply
{

	internal interface IRequestListener {

		void OnRequestReceived(string requestId, string request);
	}

	internal interface IExceptionListener {

		void OnException(Exception exception);
	}
	
	internal class RequestManager {
		private static ILog _log= LogManager.GetLogger("Lightstreamer.DotNet.RequestReply.Requests");

		private string _name;
		
		private TextReader _reader;

		private MessageSender _replySender;

		private IRequestListener _requestListener;
		private IExceptionListener _exceptionListener;

		private bool _stop;

		public RequestManager(string name, Stream requestStream, Stream replyStream, WriteState sharedWriteState, int keepaliveMillis, IRequestListener requestListener, IExceptionListener exceptionListener) {
			_name= name;

			_reader= new StreamReader(requestStream, Encoding.UTF8);

			_replySender= new MessageSender(name, replyStream, sharedWriteState, true, keepaliveMillis, exceptionListener);

			_requestListener= requestListener;
			_exceptionListener= exceptionListener;

			_stop= false;
		}

		public void ChangeKeepalive(int keepaliveMillis, bool alsoInterrupt) {
			_replySender.ChangeKeepalive(keepaliveMillis, alsoInterrupt);
		}

		public void StartOut() {
			_replySender.StartOut();
		}

		public void StartIn() {
			Thread t= new Thread(new ThreadStart(Run));
			t.Start();
		}

		public void Run() {
			_log.Info("Request receiver '" + _name + "' starting...");
			
			do {
				string line= null;
				try {
					line= _reader.ReadLine();
				
					if (_log.IsDebugEnabled) {
						_log.Debug("Request line: " + line);
					}
				} 
				catch (Exception e) {
					if (_stop) break;

					_exceptionListener.OnException(new RemotingException("Exception caught while reading from the request stream: " + e.Message, e));
					break;
				}
				
				if (_stop) break;

				if (line == null) {
					_exceptionListener.OnException(new RemotingException("Unexpected end of request stream reached", new EndOfStreamException()));
					break;
				}
				
				OnRequestReceived(line);
				
			} while (!_stop);

			_log.Info("Request receiver '" + _name + "' stopped");
		}

		public void Quit() {
			_stop= true;

			_replySender.Quit();
		}

		public void SendReply(string requestId, string reply, ILog properLogger) {
			StringBuilder identifiedReply= new StringBuilder();
			identifiedReply.Append(requestId);
			identifiedReply.Append(RemotingProtocol.SEP);
			identifiedReply.Append(reply);

			reply= identifiedReply.ToString();
			properLogger.Debug("Processed request: " + requestId);

			_replySender.SendMessage(reply);
		}

		public void SendUnsolicitedMessage(string virtualRequestId, string msg, ILog properLogger) {
			StringBuilder identifiedReply= new StringBuilder();
			identifiedReply.Append(virtualRequestId);
			identifiedReply.Append(RemotingProtocol.SEP);
			identifiedReply.Append(msg);

			msg= identifiedReply.ToString();
			properLogger.Debug("Sending unsolicited message");

			_replySender.SendMessage(msg);
		}

		private void OnRequestReceived(string request) {
			int sep= request.IndexOf(RemotingProtocol.SEP);
			if (sep < 1) {
				_log.Warn("Discarding malformed request: " + request);
				return;
			}
			
			string requestId= request.Substring(0, sep);

			_requestListener.OnRequestReceived(requestId, request.Substring(sep +1));
		}
	}

	internal class WriteState {
		internal MessageSender lastWriter = null;
	}

	internal class MessageSender {
		private static ILog _replog= LogManager.GetLogger("Lightstreamer.DotNet.RequestReply.Replies");
		private static ILog _notlog= LogManager.GetLogger("Lightstreamer.DotNet.RequestReply.Notifications");
		private static ILog _keeplog = LogManager.GetLogger("Lightstreamer.DotNet.RequestReply.Keepalives");

		private string _name;

		private long _jan1_1970_utc_ticks;

		private LinkedList<string> _queue;
		private TextWriter _writer;
		private WriteState _writeState;
		private bool _forReplies;
		private volatile int _keepaliveMillis;

		private IExceptionListener _exceptionListener;

		private bool _stop;
	
		public MessageSender(string name, Stream stream, WriteState sharedWriteState, int keepaliveMillis, IExceptionListener exceptionListener) :
			this(name, stream, sharedWriteState, false, keepaliveMillis, exceptionListener) {}

		public MessageSender(string name, Stream stream, WriteState sharedWriteState, bool forReplies, int keepaliveMillis, IExceptionListener exceptionListener) {
			_name= name;

			DateTime jan1_1970= new DateTime(1970, 1, 1, 1, 00, 00, 00);
			DateTime jan1_1970_utc= jan1_1970.ToUniversalTime();
			_jan1_1970_utc_ticks= jan1_1970_utc.Ticks;

			_queue= new LinkedList<string>();
			UTF8Encoding noBomEncoding = new UTF8Encoding(false);
			_writer = new StreamWriter(stream, noBomEncoding);
	        if (sharedWriteState != null) {
		        _writeState = sharedWriteState;
			} else {
				_writeState = new WriteState();
			}

			_forReplies= forReplies;
			_keepaliveMillis= keepaliveMillis;

			_exceptionListener= exceptionListener;

			_stop= false;
		}

        private ILog getProperLogger() {
            return _forReplies ? _replog : _notlog;
        }

        private String getProperType() {
            return _forReplies ? "Reply" : "Notify";
        }

		public void ChangeKeepalive(int keepaliveMillis, bool alsoInterrupt) {
			_keepaliveMillis = keepaliveMillis;
			if (alsoInterrupt) {
				// interrupts the current wait as though a keepalive were needed;
				// in most cases, this keepalive will be redundant
				lock (_queue) {
					_queue.AddLast(BaseProtocol.METHOD_KEEPALIVE);
					Monitor.Pulse(_queue);
				}
			}
		}

		public void StartOut() {
			Thread t= new Thread(new ThreadStart(Run));
			t.Start();
		}
		
		public void Run() {
            getProperLogger().Info(getProperType() + " sender '" + _name + "' starting...");

			LinkedList<string> messages= new LinkedList<string>();
			do {
				lock (_queue) {
                    if (_queue.Count == 0) {
                        if (_keepaliveMillis > 0) {
                            Monitor.Wait(_queue, _keepaliveMillis);
                        } else {
                            Monitor.Wait(_queue);
                        }
                    }

					if (_stop) 
                        break;

                    while (_queue.Count > 0) {
                        LinkedListNode<string> node = _queue.First;
                        string msg = (string)node.Value;
                        messages.AddLast(msg);

                        _queue.RemoveFirst();
                    }
				}

				if (_stop) 
                    break;

				try {
					lock (_writeState) {
						if (messages.Count == 0) {
							// the real timeout has fired
				            if (_writeState.lastWriter == null || _writeState.lastWriter == this) {
								messages.AddLast(BaseProtocol.METHOD_KEEPALIVE);
						    } else {
							    // the stream is shared and someone wrote after our last write;
								// that stream will be responsible for the next keepalive
							    continue;
							}
						}

						foreach (string msg in messages) {
                            if (msg != BaseProtocol.METHOD_KEEPALIVE) {
                                if (getProperLogger().IsDebugEnabled) {
                                    getProperLogger().Debug(getProperType() + " line: " + msg);
                                }
                            } else {
                                if (_keeplog.IsDebugEnabled) {
                                    _keeplog.Debug(getProperType() + " line: " + msg);
                                }
                            }

							_writer.WriteLine(msg);
						}

						_writer.Flush();
						_writeState.lastWriter = this;
					}
                } catch (Exception e) {
                    _exceptionListener.OnException(new RemotingException("Exception caught while writing on the " + getProperType().ToLower() + " stream: " + e.Message, e));
					break;
				}

                messages.Clear();
				
			} while (!_stop);

            getProperLogger().Info(getProperType() + " sender '" + _name + "' stopped");
		}

		public void Quit() {
			_stop= true;

			lock (_queue) {
				Monitor.Pulse(_queue);
			}
		}

		public void SendMessage(string msg) {
			if (!_forReplies) {
				long millis= (DateTime.UtcNow.Ticks - _jan1_1970_utc_ticks) / 10000;
				
				StringBuilder timedNotify= new StringBuilder();
				timedNotify.Append(millis);
				timedNotify.Append(RemotingProtocol.SEP);
				timedNotify.Append(msg);

				msg= timedNotify.ToString();
			}

			lock (_queue) {
				_queue.AddLast(msg);
				
				Monitor.Pulse(_queue);
			}
		}
	}

}
