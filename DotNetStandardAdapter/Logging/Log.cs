namespace Lightstreamer.DotNet.Server.Log
{
    //it has the name ILog because it's used in place of the log4net interface with the same name
    internal class ILog : ILogger
    {
        private static ILogger placeholder = new ILogEmpty();

        private ILogger wrappedLogger = placeholder;

        internal ILog()
        {
            //empty constructor
        }

        internal ILog(ILogger iLogger)
        {
            this.wrappedLogger = iLogger;
        }

        //always called from under the lock of the LogManager class.
        internal void setWrappedInstance(ILogger iLogger)
        {
            if (iLogger == null)
            {
                this.wrappedLogger = placeholder;
            }
            else
            {
                this.wrappedLogger = iLogger;
            }
        }

        //implement the interface:

        public void Error(string p)
        {
            this.wrappedLogger.Error(p);
        }

        public void Error(string p, System.Exception e)
        {
            this.wrappedLogger.Error(p,e);
        }

        public void Warn(string p)
        {
            this.wrappedLogger.Warn(p);
        }

        public void Warn(string p, System.Exception e)
        {
            this.wrappedLogger.Warn(p,e);
        }

        public void Info(string p)
        {
            this.wrappedLogger.Info(p);
        }

        public void Info(string p, System.Exception e)
        {
            this.wrappedLogger.Info(p,e);
        }

        public void Debug(string p)
        {
            this.wrappedLogger.Debug(p);
        }

        public void Debug(string p, System.Exception e)
        {
            this.wrappedLogger.Debug(p,e);
        }

        public void Fatal(string p)
        {
            this.wrappedLogger.Fatal(p);
        }

        public void Fatal(string p, System.Exception e)
        {
            this.wrappedLogger.Fatal(p);
        }


        public bool IsDebugEnabled
        {
            get {
                return this.wrappedLogger.IsDebugEnabled;
            }
        }

        public bool IsInfoEnabled
        {
            get {
                return this.wrappedLogger.IsInfoEnabled;
            }
        }

        public bool IsWarnEnabled
        {
            get {
                return this.wrappedLogger.IsWarnEnabled;
            }
        }

        public bool IsErrorEnabled
        {
            get {
                return this.wrappedLogger.IsErrorEnabled;
            }
        }

        public bool IsFatalEnabled
        {
            get
            {
                return this.wrappedLogger.IsFatalEnabled;
            }
        }

        
    }

    internal class ILogEmpty : ILogger
    {
        public void Error(string p)
        {
        }

        public void Error(string p, System.Exception e)
        {
        }

        public void Warn(string p)
        {
        }

        public void Warn(string p, System.Exception e)
        {
        }

        public void Info(string p)
        {
        }

        public void Info(string p, System.Exception e)
        {
        }

        public void Debug(string p)
        {
        }

        public void Debug(string p, System.Exception e)
        {
        }

        public void Fatal(string p)
        {
        }

        public void Fatal(string p, System.Exception e)
        {
        }


        public bool IsDebugEnabled
        {
            get { return false; }
        }

        public bool IsInfoEnabled
        {
            get { return false; }
        }

        public bool IsWarnEnabled
        {
            get { return false; }
        }

        public bool IsErrorEnabled
        {
            get { return false; }
        }

        public bool IsFatalEnabled
        {
            get { return false; }
        }
    }
}
