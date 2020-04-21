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
	
	internal class RequestReceiver {
		private static ILog _log= LogManager.GetLogger("Lightstreamer.DotNet.Server.RequestReply.Requests");

		private string _name;
		
		private TextReader _reader;

		private NotifySender _replySender;

		private IRequestListener _requestListener;
		private IExceptionListener _exceptionListener;

		private bool _stop;

		public RequestReceiver(string name, Stream requestStream, Stream replyStream, int keepaliveMillis, IRequestListener requestListener, IExceptionListener exceptionListener) {
			_name= name;

			_reader= new StreamReader(requestStream);

			_replySender= new NotifySender(name, replyStream, true, keepaliveMillis, exceptionListener);

			_requestListener= requestListener;
			_exceptionListener= exceptionListener;

			_stop= false;
		}

		public void ChangeKeepalive(int keepaliveMillis, bool alsoInterrupt) {
			_replySender.ChangeKeepalive(keepaliveMillis, alsoInterrupt);
		}

		public void Start() {
			_replySender.Start();

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

			_replySender.SendNotify(reply);
		}

		public void SendUnsolicitedMessage(string virtualRequestId, string msg, ILog properLogger) {
			StringBuilder identifiedReply= new StringBuilder();
			identifiedReply.Append(virtualRequestId);
			identifiedReply.Append(RemotingProtocol.SEP);
			identifiedReply.Append(msg);

			msg= identifiedReply.ToString();
			properLogger.Debug("Sending unsolicited message");

			_replySender.SendNotify(msg);
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

	internal class NotifySender {
		private static ILog _replog= LogManager.GetLogger("Lightstreamer.DotNet.Server.RequestReply.Replies");
		private static ILog _notlog= LogManager.GetLogger("Lightstreamer.DotNet.Server.RequestReply.Notifications");
		private static ILog _repKlog = LogManager.GetLogger("Lightstreamer.DotNet.Server.RequestReply.Replies.Keepalives");
		private static ILog _notKlog = LogManager.GetLogger("Lightstreamer.DotNet.Server.RequestReply.Notifications.Keepalives");

		private string _name;

		private long _jan1_1970_utc_ticks;

		private LinkedList<string> _queue;
		private TextWriter _writer;
		private bool _repliesNotNotifies;
		private volatile int _keepaliveMillis;

		private IExceptionListener _exceptionListener;

		private bool _stop;
	
		public NotifySender(string name, Stream notifyStream, int keepaliveMillis, IExceptionListener exceptionListener) :
			this(name, notifyStream, false, keepaliveMillis, exceptionListener) {}

		public NotifySender(string name, Stream notifyStream, bool repliesNotNotifies, int keepaliveMillis, IExceptionListener exceptionListener) {
			_name= name;

			DateTime jan1_1970= new DateTime(1970, 1, 1, 1, 00, 00, 00);
			DateTime jan1_1970_utc= jan1_1970.ToUniversalTime();
			_jan1_1970_utc_ticks= jan1_1970_utc.Ticks;

			_queue= new LinkedList<string>();
			_writer= new StreamWriter(notifyStream);
			_repliesNotNotifies= repliesNotNotifies;
			_keepaliveMillis= keepaliveMillis;

			_exceptionListener= exceptionListener;

			_stop= false;
		}

        private ILog getProperLogger() {
            return _repliesNotNotifies ? _replog : _notlog;
        }

        private ILog getProperKeepaliveLogger() {
            return _repliesNotNotifies ? _repKlog : _notKlog;
        }

        private String getProperType() {
            return _repliesNotNotifies ? "Reply" : "Notify";
        }

		public void ChangeKeepalive(int keepaliveMillis, bool alsoInterrupt) {
			_keepaliveMillis = keepaliveMillis;
			if (alsoInterrupt) {
				// interrupts the current wait as though a keepalive were needed;
				// in most cases, this keepalive will be redundant
				lock (_queue) {
					Monitor.Pulse(_queue);
				}
			}
		}

		public void Start() {
			Thread t= new Thread(new ThreadStart(Run));
			t.Start();
		}
		
		public void Run() {
            getProperLogger().Info(getProperType() + " sender '" + _name + "' starting...");

			LinkedList<string> notifies= new LinkedList<string>();
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

                    if (_queue.Count == 0) {
                        // the timeout (real or simulated) has fired
                        notifies.AddLast(RemotingProtocol.METHOD_KEEPALIVE);

                    } else {
                        while (_queue.Count > 0) {
                            LinkedListNode<string> node = _queue.First;
                            string reply = (string)node.Value;
                            notifies.AddLast(reply);

                            _queue.RemoveFirst();
                        }
                    }
				}

				if (_stop) 
                    break;

				try {
					foreach (string notify in notifies) {
                        if (getProperLogger().IsDebugEnabled) {
                            if (notify != RemotingProtocol.METHOD_KEEPALIVE) {
                                getProperLogger().Debug(getProperType() + " line: " + notify);
                            } else {
                                getProperKeepaliveLogger().Debug(getProperType() + " line: " + notify);
                            }
                        }

						_writer.WriteLine(notify);
					}

					_writer.Flush();

                } catch (Exception e) {
                    _exceptionListener.OnException(new RemotingException("Exception caught while writing on the " + getProperType().ToLower() + " stream: " + e.Message, e));
					break;
				}

                notifies.Clear();
				
			} while (!_stop);

            getProperLogger().Info(getProperType() + " sender '" + _name + "' stopped");
		}

		public void Quit() {
			_stop= true;

			lock (_queue) {
				Monitor.Pulse(_queue);
			}
		}

		public void SendNotify(string notify) {
			if (!_repliesNotNotifies) {
				long millis= (DateTime.UtcNow.Ticks - _jan1_1970_utc_ticks) / 10000;
				
				StringBuilder timedNotify= new StringBuilder();
				timedNotify.Append(millis);
				timedNotify.Append(RemotingProtocol.SEP);
				timedNotify.Append(notify);

				notify= timedNotify.ToString();
			}

			lock (_queue) {
				_queue.AddLast(notify);
				
				Monitor.Pulse(_queue);
			}
		}
	}

}
