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

using System.Collections;
using System.Collections.Generic;
namespace Lightstreamer.DotNet.Server.Log
{
    internal class LogManager
    {
        private static IDictionary<string, ILog> logInstances = new Dictionary<string, ILog>();

        private static ILoggerProvider currentLoggerProvider = null;

        internal static void SetLoggerProvider(ILoggerProvider ilp) {
            lock (logInstances)
            {
                currentLoggerProvider = ilp;

                foreach (KeyValuePair<string, ILog> aLog in logInstances)
                {
                    if (ilp == null)
                    {
                        aLog.Value.setWrappedInstance(null);
                    }
                    else
                    {
                        aLog.Value.setWrappedInstance(currentLoggerProvider.GetLogger(aLog.Key));
                    }
                }
            }
        }

        internal static ILog GetLogger(string category)
        {
            lock (logInstances)
            {
                if (!logInstances.ContainsKey(category))
                {
                    if (currentLoggerProvider != null)
                    {
                        logInstances[category] = new ILog(currentLoggerProvider.GetLogger(category));
                    }
                    else
                    {
                        logInstances[category] = new ILog();
                    }

                }
                return logInstances[category];
            }
        }


    }


}
