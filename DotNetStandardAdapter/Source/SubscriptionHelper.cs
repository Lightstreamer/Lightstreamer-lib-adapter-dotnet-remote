using System;
using System.Collections;
using System.Configuration;
using System.Threading;
using System.Text;

using System.Threading.Tasks;

using Lightstreamer.DotNet.Server.Log;

namespace Lightstreamer.DotNet.Server
{
	internal interface MyTask
	{
		string getCode();
		bool DoTask();
		void DoLateTask();
	}

    internal class SubscriptionHelper
	{
		private static ILog _log = LogManager.GetLogger("Lightstreamer.DotNet.Server.SubscriptionHelper");

		private IDictionary _activeItems;

        private enum ConcurrencyPolicies {
            SystemPool,
            Unlimited
        }

        private ConcurrencyPolicies _concurrencyPolicy;

        public SubscriptionHelper() {
            _activeItems = new Hashtable();

            System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
            string policy = appSettings["Lightstreamer.Data.Concurrency"];
            if (policy != null) {
                try {
                    _concurrencyPolicy = (ConcurrencyPolicies)Enum.Parse(typeof(ConcurrencyPolicies), policy);
                } catch (Exception) {
                    throw new Exception("Invalid Lightstreamer.Data.Concurrency configuration: " + policy);
                }
            } else {
                _concurrencyPolicy = ConcurrencyPolicies.Unlimited;
            }
        }

		public void DoSubscription(string itemName, MyTask subscriptionTask)
		{
			// we are still in the request dequeueing thread,
			// hence we know that the invocations of DoSubscription
			// and DoUnsubscription are sequential for the same item;
			// to prevent blocking this thread, we will invoke the
			// Subscribe() on the Adapter in a different thread;
			// but we will enqueue the requests pertaining to the same
			// item, so as to guarantee sequentiality to the Adapter too

			SubscrData data;
			lock (_activeItems) {
				data = (SubscrData) _activeItems[itemName];
				if (data == null) {
					data = new SubscrData(this, itemName);
					_activeItems.Add(itemName, data);
				}
				data._queued++;
					// this would prevent the removal of the element
					// in case the dequeuing thread should end right now
			}
			data.AddTask(subscriptionTask, true);
		}

		public void DoUnsubscription(string itemName, MyTask subscriptionTask)
		{
			// we are still in the request dequeueing thread,
			// hence we know that the invocations of DoSubscription
			// and DoUnsubscription are sequential for the same item;
			// to prevent blocking this thread, we will invoke the
			// Unsubscribe() on the Adapter in a different thread;
			// but we will enqueue the requests pertaining to the same
			// item, so as to guarantee sequentiality to the Adapter too
			
			SubscrData data;
			lock (_activeItems) {
				data = (SubscrData) _activeItems[itemName];
				if (data == null) {
					// impossible, unless the corresponding subscription request
					// got lost; in fact, it should have created the element
					// and set _queued; and the dequeuer can have reset _queued
					// only after setting _code; under such conditions,
					// the element cannot have been eliminated
					_log.Error("Task list expected for item " + itemName);
					return;
				}
				data._queued++;
					// this would prevent the removal of the element
					// in case the dequeuing thread should end right now
				}
			data.AddTask(subscriptionTask, false);
		}

		public string GetSubscriptionCode(string itemName)
		{
			lock (_activeItems) {
				SubscrData data = (SubscrData) _activeItems[itemName];
				if (data != null) {
					return data._code;
						// it may be null, in case an unsubscription
						// has just finished but a new subscription
						// has already been enqueued
				} else {
					return null;
				}
			}
		}

        public string getConcurrencyPolicy() {
            return _concurrencyPolicy.ToString();
        }

		private class SubscrData
		{
			public SubscriptionHelper _container;
			public string _itemName;
			public int _queued;  // will be synchronized with items
			public string _code;  // will be synchronized with items
			public Queue _tasks;
			public bool _subscrExpected;
			public bool _running;
			public bool _lastSubscrOutcome;

			public SubscrData(SubscriptionHelper container, string itemName)
			{
				_container = container;
				_itemName = itemName;
				_tasks = new Queue();
				_subscrExpected = true;
				_queued = 0;
				_code = null;
				_running = false;
			}

			public void AddTask(MyTask task, bool isSubscr)
			{
				if (isSubscr != (task.getCode() != null)) {
					// impossible, unless DataProviderServer were bugged
					_log.Error("Inconsistent task for item " + _itemName);
				}
				lock (this) {
					if (isSubscr != _subscrExpected) {
						// impossible, unless the sequence of requests
						// to the Remote Server were wrong
						_log.Error("Unexpected task for item " + _itemName);
					}
					_tasks.Enqueue(task);
						// _queued has already been incremented by the caller
					_subscrExpected = ! isSubscr;
					if (!_running) {
						// only one dequeuer can be active
						_running = true;
                        if (false) {
                            // placeholder
                        } else if (_container._concurrencyPolicy == ConcurrencyPolicies.SystemPool) {
                            TaskCreationOptions blocking = TaskCreationOptions.PreferFairness;
                            /*
                            if (isSubscr) {
                                blocking |= TaskCreationOptions.LongRunning;
                            }
                            We could do this to take into account that subscription tasks
                            are potentially blocking,
                            but we prefer to leverage the thread pool limit also in this case;
                            to enforce separate threads for the potentially blocking tasks
                            you can only resort to the Unlimited setting
                            */
                            Task.Factory.StartNew(() => Dequeue(), blocking);
                        } else { // ConcurrencyPolicies.Unlimited
                            Thread thread = new Thread(new ThreadStart(Dequeue));
                            thread.Start();
                        }
					}
				}
			}

			public void Dequeue()
			{
				int dequeued = 0;
				bool lastSubscrOutcome = true;
					// it will be first taken from the state anyway
				while (true) {
					MyTask task;
					bool isLast;
					lock (this) {
						if (dequeued == 0) {
							lastSubscrOutcome = _lastSubscrOutcome;
								// initial state
						}
						if (_tasks.Count == 0) {
							_lastSubscrOutcome = lastSubscrOutcome;
								// final state
							_running = false;
							break;
							// from this moment it is possible that a new
							// dequeuer gets started
						}
						task = (MyTask) _tasks.Dequeue();
						isLast = (_tasks.Count == 0);
						dequeued++;
					}
					// we will invoke the subscribe/unsubscribe without holding the lock
					try {
						string code = task.getCode();
						if (code != null) {
							// IT'S A SUBSCRIPTION
							// ASSERT (either it is the first event, or it was preceded by an unsubscription)
							if (! isLast) {
								// ASSERT (it will be followed by an unsubscription)
								task.DoLateTask();
								lastSubscrOutcome = false;
									// on the next iteration we will dequeue the unsubscription,
									// again with DoLateTask
							} else {
								lock (_container._activeItems) {
									_code = code;
									// from this moment, the received updates will be
									// associated with this subscription; should we receive
									// late updates meant for a previous subscription,
									// they would be misinterpreted; it's the Adapter
									// responsible for ensuring that this never happens,
									// that is, that no update for this item is sent after
									// the termination of the Unsubscribe() invocation
								}
								lastSubscrOutcome = task.DoTask();
									// if it return false, i.e. the subscription
									// has failed, we won't invoke Unsubscribe()
							}
						} else {
							// IT'S AN UNSUBSCRIPTION
							// ASSERT(the event was preceded by a subscription)
							if (lastSubscrOutcome) {
								task.DoTask();
								// we don't care if it was successful or not;
								// an unsuccessful Unsubscribe doesn't propagate effects
							} else {
								// either the previous subscription failed
								// or it was obsolete and not invoked at all
								task.DoLateTask();
							}
							lock (_container._activeItems) {
								_code = null;
								// from this moment any update received from the Adapter
								// will be ignored; however, the Adapter should ensure
								// that no update for this item is sent after
								// the termination of the Unsubscribe() invocation
							}
						}
					} catch (Exception e) {
						_log.Error("Unexpected error: " + e);
					}
				}

				lock (_container._activeItems) {
					_queued -= dequeued;
					// as long as the item is subscribed to, the element should be kept;
					// if the item was unsubscribed from, the element should be removed,
					// unless a new subscription request has already been received;
					// in the latter case, _queued cannot be zero
					if (_code == null && _queued == 0) {
						SubscrData data = (SubscrData) _container._activeItems[_itemName];
						if (data == null) {
							// it can happen, in case this dequeueing thread
							// was stopped just above and has been preceded
							// by a new subscribe/unsubscribe pair
							// with a related new dequeueing thread
						} else if (data != this) {
							// event this can happen, if, in the above case,
							// also the second thread was stopped just above
							// and has been preceded by a new subscribe/unsubscribe pair
							// with a related new dequeueing thread
						} else {
							_container._activeItems.Remove(_itemName);
						}
					} else {
						// we can exit safely, because new events are bound to come
						// or to be dequeued by a new dequeueing thread,
						// which will have a new opportunity to remove this element
					}
				}
			}
		}
	}

}
