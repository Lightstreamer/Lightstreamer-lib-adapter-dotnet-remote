# Lightstreamer Changelog - Reusable Metadata Adapters - .NET Adapter

## 1.12.1 <i>Released on 21 Jan 2020</i>

<i>Compatible with Adapter Remoting Infrastructure since 1.8.</i>
<i>Compatible with code developed with the previous version.</i>

Fixed the default handling of parallelization of request processing, which was
actually spawning a thread for each request, rather than submitting the requests
to the default system task scheduling pool. This may have caused a significant
overload in cases of high request rate.

Modified the handling of the keepalives when connected to a Proxy Adapter
(i.e. Adapter Remoting Infrastructure) version 1.9 or higher:
the preferred keepalive interval requested by the Proxy Adapter, when stricter
than the configured one, is now obeyed (with a safety minimun of 1 second).
Moreover, in that case, the default interval configuration is now 10 seconds
instead of 1.
<b>COMPATIBILITY NOTE:</b> If an existing installation relies
on a very short keepalive interval to keep the connection alive due to intermediate
nodes, the time should now be explcitly configured.

Deprecated the constructors of DataProviderServer and MetadataProviderServer
that allow for the specification of initializeOnStart as true.
These constructors will be removed in a future update, as the initializeOnStart
flag was just meant as a temporary backward compatibility trick.
<b>COMPATIBILITY NOTE:</b> Existing code and binaries
using the deprecated constructors are still supported, but it is recommended
to align the code. See the notes in the constructor documentations for details.
Improved the documentation of the Start method of the DataProviderServer and
MetadataProviderServer classes, to clarify the behavior.

Added clarifications in the documentation of the exception handlers and fix
a few obsolete notes.


## 1.12.0 build 1030 <i>Released on 28 Jan 2019</i>

<i>Compatible with Adapter Remoting Infrastructure since 1.8.</i>
<i>Compatible with code developed with the previous version.</i>
<i>May not be compatible with the deployment structure of the previous version; see the compatibility notes below.</i>

Renamed the SDK, which was named "SDK for .NET Adapters",
to "SDK for .NET Standard Adapters". The new name emphasizes that it is now
compliant with .NET Standard API Specifications 2.0.
The .NET Standard allows greater uniformity through the .NET
ecosystem and works seamlessly with .NET Core, .NET Framework,
Mono, and UWP apps.
<b>COMPATIBILITY NOTE:</b> Existing applications that target the following platforms:

- .NET Framework (>=4.6)</li>
- .NET Core</li>
- .NET Standard</li>

can be upgraded to the new SDK provided that the
platform version is compatible with .NET Standard 2.0. Otherwise they have to
stick to the older SDK.

Made the library available through the NuGet online service. For previous versions,
the binaries were included in Lightstreamer distribution package.

Removed the DotNetServer.exe utility, which allowed for the configuration and launch
of single Remote Adapter instances, with a basic handling of connections and errors.
<b>COMPATIBILITY NOTE:</b> Current istallations that take
advantage of the DotNetServer.exe utility should be integrated with a custom launcher;
see the provided demos, which all include simple custom launchers for the adapters,
with the related source code.

Extended DataProviderServer and MetadataProviderServer (through the Server superclass)
with properties for credentials, to be sent to the Proxy Adapter upon each connection.
Credential check is an optional configuration of the Proxy Adapter; if not leveraged,
the credentials will be ignored.

Added full support for ARI Protocol extensions introduced in Adapter Remoting Infrastructure
version 1.9.
<b>COMPATIBILITY NOTE:</b> If Adapter Remoting Infrastructure 1.8.x
(corresponding to Server 7.0.x) is used and credentials to be sent to the Proxy Adapter
are specified, they will obviously be ignored, but the Proxy Adapter will issue some
log messages at WARN level on startup.
<b>COMPATIBILITY NOTE:</b> Only in the very unlikely case
that Adapter Remoting Infrastructure 1.8.x (corresponding to Server 7.0.x) were used
and a custom remote parameter named "ARI.version" were defined in adapters.xml,
this SDK would not be compatible with Lightstreamer Server, hence the Server should be upgraded
(or a different parameter name should be used).

Embedded the sample app.config file in the docs package, for convenience.