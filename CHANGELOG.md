# Lightstreamer Changelog - SDK for .NET Standard Adapters



## 1.12.2 - <i>Released on 25 May 2021</i>

<i>Compatible with Adapter Remoting Infrastructure since Server version 7.0.</i><br/>
<i>Compatible with code developed with the previous version.</i>

Reformulated the compatibility constraint with respect to the Server version,
instead of the Adapter Remoting Infrastructure version.

Added the source code of the sample LiteralBasedProvider, whose binary was already included
in the generated library.


## 1.12.1 - <i>Released on 21 Jan 2020</i>

<i>Compatible with Adapter Remoting Infrastructure since 1.8 (corresponding to Server 7.0).</i><br/>
<i>Compatible with code developed with the previous version.</i>

Fixed the default handling of parallelization of request processing, which was
actually spawning a thread for each request, rather than submitting the requests
to the default system task scheduling pool. This may have caused a significant
overload in cases of high request rate.

Modified the handling of the keepalives when connected to a Proxy Adapter
(i.e. Adapter Remoting Infrastructure) version 1.9 (corresponding to Server 7.1) or higher:
the preferred keepalive interval requested by the Proxy Adapter, when stricter
than the configured one, is now obeyed (with a safety minimun of 1 second).
Moreover, in that case, the default interval configuration is now 10 seconds
instead of 1.<br/>
<b>COMPATIBILITY NOTE:</b> <i>If an existing installation relies
on a very short keepalive interval to keep the connection alive due to intermediate
nodes, the time should now be explcitly configured.</i>

Deprecated the constructors of DataProviderServer and MetadataProviderServer
that allow for the specification of initializeOnStart as true.
These constructors will be removed in a future update, as the initializeOnStart
flag was just meant as a temporary backward compatibility trick.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Existing code and binaries
using the deprecated constructors are still supported, but it is recommended
to align the code. See the notes in the constructor documentations for details.</i>

Improved the documentation of the Start method of the DataProviderServer and
MetadataProviderServer classes, to clarify the behavior.

Added clarifications in the documentation of the exception handlers and fix
a few obsolete notes.


## 1.12.0 - <i>Released on 28 Jan 2019</i>

<i>Compatible with Adapter Remoting Infrastructure since 1.8 (corresponding to Server 7.0).</i><br/>
<i>Compatible with code developed with the previous version.</i><br/>
<i>May not be compatible with the deployment structure of the previous version; see the compatibility notes below.</i>

Renamed the SDK, which was named "SDK for .NET Adapters",
to "SDK for .NET Standard Adapters". The new name emphasizes that it is now
compliant with .NET Standard API Specifications 2.0.
The .NET Standard allows greater uniformity through the .NET
ecosystem and works seamlessly with .NET Core, .NET Framework,
Mono, and UWP apps.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Existing applications that target the following platforms:

- .NET Framework (>=4.6)</li>
- .NET Core</li>
- .NET Standard</li>

can be upgraded to the new SDK provided that the
platform version is compatible with .NET Standard 2.0. Otherwise they have to
stick to the older SDK.</i>

Made the library available through the NuGet online service. For previous versions,
the binaries were included in Lightstreamer distribution package.

Removed the DotNetServer.exe utility, which allowed for the configuration and launch
of single Remote Adapter instances, with a basic handling of connections and errors.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Current istallations that take
advantage of the DotNetServer.exe utility should be integrated with a custom launcher;
see the provided demos, which all include simple custom launchers for the adapters,
with the related source code.</i>

Extended DataProviderServer and MetadataProviderServer (through the Server superclass)
with properties for credentials, to be sent to the Proxy Adapter upon each connection.
Credential check is an optional configuration of the Proxy Adapter; if not leveraged,
the credentials will be ignored.

Added full support for ARI Protocol extensions introduced in Adapter Remoting Infrastructure
version 1.9 (corresponding to Server 7.1).<br/>
<b>COMPATIBILITY NOTE:</b> <i>If Adapter Remoting Infrastructure 1.8.x
(corresponding to Server 7.0.x) is used and credentials to be sent to the Proxy Adapter
are specified, they will obviously be ignored, but the Proxy Adapter will issue some
log messages at WARN level on startup.</i><br/>
<b>COMPATIBILITY NOTE:</b> <i>Only in the very unlikely case
that Adapter Remoting Infrastructure 1.8.x (corresponding to Server 7.0.x) were used
and a custom remote parameter named "ARI.version" were defined in adapters.xml,
this SDK would not be compatible with Lightstreamer Server, hence the Server should be upgraded
(or a different parameter name should be used).</i>

Embedded the sample app.config file in the docs package, for convenience.


## 1.11.0 - <i>Released on 28 Feb 2018</i>

<i>Compatible with Adapter Remoting Infrastructure since 1.8 (corresponding to Server 7.0).</i><br/>
<i>Compatible with code developed with the previous version.</i>

Added clarifications on licensing matters in the docs.


## 1.11.0 - <i>Released on 20 Dec 2017</i>

<i>Compatible with Adapter Remoting Infrastructure since 1.8 (corresponding to Server 7.0).</i><br/>
<i>May not be compatible with code developed with the previous version; see compatibility notes below.</i>

Modified the interface in the part related to Mobile Push Notifications,
after the full revision of Lightstreamer Server's MPN Module. In particular:
 - Modified the signature of the NotifyMpnDeviceAccess and
NotifyMpnDeviceTokenChange methods of the MetadataProvider interface,
to add a session ID argument.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Existing Remote Metadata Adapter
source code has to be ported in order to be compiled with the new dll,
unless the Adapter class inherits from the supplied MetadataProviderAdapter
or LiteralBasedProvider and the above methods are not defined.<br/>
On the other hand, existing Remote Metadata Adapter binaries still run
with the new version of Lightstreamer Server and the Proxy Adapters,
as long as they keep being hosted by an old version of the .NET Adapter SDK
and the MPN Module is disabled.
Otherwise, they should be ported to the new SDK version and recompiled.</i>
 - Revised the public constants defined in the MpnPlatformType class.
The constants referring to the supported platforms have got new names and
corresponding new values, whereas the constants for platforms not yet
supported have been removed.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Existing Remote Metadata Adapters
explicitly referring to the constants have to be aligned.
Even if just testing the values of the MpnPlatformType objects received
as MpnDeviceInfo.Type, they still have to be aligned.</i>
 - Removed the subclasses of MpnSubscriptionInfo (namely
MpnApnsSubscriptionInfo and MpnGcmSubscriptionInfo) that were used
by the SDK library to supply the attributes of the MPN subscriptions
in NotifyMpnSubscriptionActivation. Now, simple instances of
MpnSubscriptionInfo will be supplied and attribute information can be
obtained through the new NotificationFormat property.
See the MPN chapter on the General Concepts document for details on the
characteristics of the Notification Format.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Existing Remote Metadata Adapters
leveraging NotifyMpnSubscriptionActivation and inspecting the supplied
MpnSubscriptionInfo have to be ported to the new class contract.</i>
 - Added equality checks based on the content in MpnDeviceInfo and MpnSubscriptionInfo.
 - Improved the interface documentation in various parts.

Added checks to protect the MetadataProviderServer and DataProviderServer objects from reuse, which is forbidden.

Clarified in the docs for notifySessionClose which race conditions with other methods can be expected.

Aligned the documentation to comply with current licensing policies.


## 1.10.0 - Released on 23 Jan 2017

<i>Compatible with Adapter Remoting Infrastructure since 1.7 (corresponding to Server 6.0).</i><br/>
<i>Compatible with code developed with the previous version.</i>

Improved the app configuration example, by showing how to configure the keepalive messages.


## 1.10.0 - Released on 10 May 2016

<i>Compatible with Adapter Remoting Infrastructure since 1.7 (corresponding to Server 6.0).</i><br/>
<i>May not be compatible with code developed with the previous version; see compatibility notes below.</i>

Dropped the compatibility with .NET environment versions prior to 4.0. 4.0 or later is now required.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Check the .NET runtime
environment in use before updating existing Remote Adapter installations.
If the Remote Adapter is run by a custom launcher, and if the application
had been compiled for an earlier .NET environment, the loading of the new
library may fail. In this case, the application has to be recompiled and
possibly ported to a 4.0 or later target environment.</i>

As a consequence of the new runtime requirements, the names of the provided
exe and dll files have changed, to lose the _N2 suffix.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Existing Remote Adapter
installations may require some renaming within some custom script.
If the Remote Adapter is run by a custom launcher, a rebuild of the
application may be needed to refer to the new dll name.</i>

Introduced parallelization of the invocations of methods on the Remote
Metadata Adapter; in fact, previously, the invocations were always done
sequentially, with possible inefficient use of the available resources.
Several policies are now available for both Metadata and Data Adapter method
invocation, and can be configured through the application configuration;
see the new sample file in the new "conf" directory for details.<br/>
By default, the invocations to the Metadata Adapter methods are now done
in parallel.<br/>
<b>COMPATIBILITY NOTE:</b> <i>If existing
Remote Metadata Adapters don't support concurrent invocations, sequential
invocations should be restored by configuration.</i>

Introduced the possibility to configure the keepalive time (which was fixed
to one second) through the application configuration; see the new sample
file in the new "conf" directory for details.

Improved logging; now the keepalives can be excluded from the the detailed log of request, reply and notification messages.

Fixed obsolete notes in the docs for DataProvider and MetadataProvider interfaces on the way implementations are supplied.


## 1.9 - Released on 16 Jul 2015

<i>Compatible with Adapter Remoting Infrastructure since 1.7 (corresponding to Server 6.0).</i><br/>
<i>Compatible with code developed with the previous version.</i>

Improved logging; now the detailed log of request-reply messages not including notification messages (i.e. data updates) is possible.

Reduced some long pathnames in the docs pages, which could cause issues on some systems.


## 1.9 - Released on 21 Jan 2015

<i>Compatible with Adapter Remoting Infrastructure since 1.7 (corresponding to Server 6.0).</i><br/>
<i>May not be compatible with code developed with the previous version; see compatibility notes below.</i>

Introduced the possibility to provide Adapter initialization parameters
directly from the Proxy Adapter configuration; such parameters (to be
supplied as explained in the Adapter Remoting Infrastructure documentation)
will be added to those already received by the Metadata or Data Adapter
Init method.<br/>
As a consequence, the Init method of a Remote Adapter is no longer invoked
upon Remote Server startup, but only after the connection with the Proxy
Adapter has been established.<br/>
<b>COMPATIBILITY NOTE:</b>
<i>The move of initialization stuff performed in the Init method from before
to after the connection attempt may be undesirable. If this is the case:
- if the Remote Adapter is run through the supplied DotNetServer_N2.exe,
the new /initonstart argument can be added to the command line, to restore
the old behavior;
- if the Remote Adapter is run by a custom launcher, the same can be
achieved by creating the involved "MetadataProviderServer" or "DataProviderServer"
with the new 1-argument constructor (see the docs);
- alternatively, with a custom launcher, any initialization stuff that
must precede the connection may be moved out of the Adapter Init method,
into a custom method of the Adapter class that can be directly invoked
by the launcher before the invocation of Start on the involved
"MetadataProviderServer" or "DataProviderServer";
this would keep the advantages of the extension.</i>

Extended the MetadataProvider interface to support the new
Push Notification Service (aka MPN Module). When enabled, the new methods will be
invoked in order to validate client requests related with the service. See the
interface docs for details.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Existing Remote Metadata Adapter
source code has to be extended in order to be compiled with the new dll
(the new methods could just throw a NotificationException),
unless the Adapter class inherits from one of the supplied LiteralBasedProvider
or MetadataProviderAdapter. In the latter case, the Adapter will accept
any MPN-related request; however, MPN-related client requests can be satisfied
only if the involved "app" has been properly configured.<br/>
On the other hand, existing Remote Metadata Adapter binaries hosted by an old
version of the .NET Adapter SDK still run with the new version of Lightstreamer
Server and the Proxy Adapters, as long as the MPN Module is not enabled.</i>

Introduced the "ClearSnapshot" operations on the Remote Server's IItemEventListener,
for clearing the state of an item in a single step (or, in DISTINCT mode, for
notifying compatible clients that the update history should be discarded).
See the interface docs for details.<br/>
<b>COMPATIBILITY NOTE:</b> <i>Existing Data Adapters don't need to be extended or recompiled.</i>

Removed the dependency of the SDK library from log4net for its own logging.
Custom launchers should use the new static SetLoggerProvider function in the
"Server" class to provide suitable consumers, by implementing new dedicated
interfaces (see the docs for details).<br/>
<b>COMPATIBILITY NOTE:</b> <i>Existing custom launchers will still run with the new
SDK library, but they won't print any log coming from the library itself;
in order to print such log, they have to be extended.
The supplied DotNetServer_N2.exe still leans on log4net, over which it
forwards all library's log.</i>
<br/>
Updated the included log4net dll to version 1.2.13; note that, according
with the above change, the log4net library is no longer required in order
to compile the Adapters, but only in order to run DotNetServer_N2.exe.

Changed DotNetServer_N2.exe so that the /host command line argument is now
mandatory; this disallows the use in combination with the "Piped" versions
of the Proxy Adapters.<br/>
<b>COMPATIBILITY NOTE:</b> <i>this is just a consequence of the discontinuation of "Piped" versions brought
by Adapter Remoting Infrastructure 1.7 (corresponding to Server 6.0); see related notes.</i>

Fixed an error in the documentation of the WinIndex property in the TableInfo class;
clarified how the field can be used to match subscription and unsubscription requests.

Simplified the included documentation, to lean on the Adapter Remoting
Infrastructure documentation for architectural aspects and on the available
examples for the deployment aspects.

Removed the examples, which are now only hosted on GitHub and managed through
the new "demos" site. Provided suitable references to find the examples there.

Modified the SDK versioning, which now differs from the internal dll versioning.


## 1.7 - Released on 20 Dec 2012

<i>Compatible with Adapter Remoting Infrastructure since 1.4.3 (corresponding to Server 5.1).</i><br/>
<i>Compatible with code developed with the previous version.</i>

Improved the performances under high update load.

Relieved the restrictions on the use of the NotifyNewTables method,
according with the locking policy change introduced in SDK for Java In-Process Adapters
version 5.1.


## 1.7 - Released on 3 Aug 2012

<i>Compatible with Adapter Remoting Infrastructure since 1.4</i><br/>
<i>Compatible with code developed with the previous version.</i>

Clarified the API documentation in several points.


## 1.7 - Released on 6 Apr 2012

<i>Compatible with Adapter Remoting Infrastructure since 1.4</i><br/>
<i>Compatible with code developed with the previous version.</i>

Fixed a bug that affected the special Update method overloads based on the
IItemEvent and IIndexedItemEvent interfaces. In case the "robust" version of the
Proxy Data Adapter had been in use, the updates could have been discarded.

Improved the extraction of HTTP headers supplied to NotifyUser.

Clarified the API documentation in several points.


## 1.7 - Released on 7 Jun 2011

<i>Compatible with Adapter Remoting Infrastructure since 1.4</i><br/>

Introduction of Lightstreamer "Duomo" release (Server 4.0).