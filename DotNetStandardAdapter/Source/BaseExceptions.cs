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

    }

}


namespace Lightstreamer.Interfaces.Data
{

    /// <summary>
    /// Base class for all exceptions directly thrown by the Data Adapter.
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

    }

}
