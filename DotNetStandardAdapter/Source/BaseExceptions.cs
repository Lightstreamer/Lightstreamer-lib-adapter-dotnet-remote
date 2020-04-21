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

namespace Lightstreamer.Interfaces.Metadata {

	/// <summary> Base class for all exceptions directly thrown by the Metadata Adapter.
	/// </summary>
	public class MetadataException : ApplicationException {

        internal MetadataException(string message, Exception innerException)
			: base(message, innerException)
        {
		}

        internal MetadataException(string message)
            : base(message)
        {
		}

#if DOCOMATIC

        /// <summary> Inherited from the Exception base class.
        /// Gets the Exception instance that caused the current exception.
        /// </summary>
        public Exception InnerException
        {
            get
            {
                return base.InnerException;
            }
        }

        /// <summary> Inherited from the Exception base class.
        /// Gets a message that describes the current exception.
        /// </summary>
        public virtual string Message
        {
            get
            {
                return base.Message;
            }
        }

        /// <summary> Inherited from the Exception base class.
        /// Gets or sets the name of the application or the object that causes the error.
        /// </summary>
        public virtual string Source
        {
            get
            {
                return base.Source;
            }
            set
            {
                base.Source = value;
            }
        }

        /// <summary> Inherited from the Exception base class.
        /// Gets a string representation of the frames on the call stack at the time the current exception was thrown.
        /// </summary>
        public virtual string StackTrace
        {
            get
            {
                return base.StackTrace;
            }
        }

        /// <summary> Inherited from the Exception base class.
        /// Gets the method that throws the current exception.
        /// </summary>
        public System.Reflection.MethodBase TargetSite
        {
            get
            {
                return base.TargetSite;
            }
        }

#endif // DOCOMATIC

    }

}


namespace Lightstreamer.Interfaces.Data
{

    /// <summary> Base class for all exceptions directly thrown by the Data Adapter.
    /// </summary>
    public class DataException : ApplicationException
    {

        internal DataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal DataException(string message)
            : base(message)
        {
        }

#if DOCOMATIC

        /// <summary> Inherited from the Exception base class.
        /// Gets the Exception instance that caused the current exception.
        /// </summary>
        public Exception InnerException
        {
            get
            {
                return base.InnerException;
            }
        }

        /// <summary> Inherited from the Exception base class.
        /// Gets a message that describes the current exception.
        /// </summary>
        public virtual string Message
        {
            get
            {
                return base.Message;
            }
        }

        /// <summary> Inherited from the Exception base class.
        /// Gets or sets the name of the application or the object that causes the error.
        /// </summary>
        public virtual string Source
        {
            get
            {
                return base.Source;
            }
            set
            {
                base.Source = value;
            }
        }

        /// <summary> Inherited from the Exception base class.
        /// Gets a string representation of the frames on the call stack at the time the current exception was thrown.
        /// </summary>
        public virtual string StackTrace
        {
            get
            {
                return base.StackTrace;
            }
        }

        /// <summary> Inherited from the Exception base class.
        /// Gets the method that throws the current exception.
        /// </summary>
        public System.Reflection.MethodBase TargetSite
        {
            get
            {
                return base.TargetSite;
            }
        }

#endif // DOCOMATIC

    }

}
