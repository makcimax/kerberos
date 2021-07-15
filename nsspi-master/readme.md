## Downloads ##

The latest release of NSspi is v0.3.1, released 5-Aug-2019. 

Version 0.3.1 adds support to obtain an IIdentity/WindowsPrinciple representing the remote connection. This is useful for servers that wish to query the properties on the principle, such as claims.

* [Source](https://github.com/antiduh/nsspi/archive/0.3.1.zip)
* [Nuget package](https://www.nuget.org/packages/NSspi)

You can also browse the list of [releases](https://github.com/antiduh/nsspi/releases).



## Introduction ##
This project provides a C# / .Net interface to the Windows Integrated Authentication API, better known as SSPI (Security Service Provider Interface). This allows a custom client / server system to authenticate users using their existing logon credentials. This allows a developer to provide Single-Sign-On in their application.

## Overview ##
The API provides raw access to authentication tokens so that authentication can be easily integrated into any networking system - you can send the tokens over a socket, a remoting interface, or heck even a serial port if you want; they're just bytes. Clients and servers may exchange encrypted and signed messages, and the server can perform client impersonation.

The project is provided as a .Net 4.0 assembly, but can just as easily be upgraded to .Net 4.5 or later. The solution file can be opened by Visual Studio 2010 SP1, Visual Studio 2012, or later Visual Studio editions.

The SSPI API provides an interface for real authentication protocols, such as Kerberos or NTLM, to be invoked transparently by client and server code in order to perform authentication and message manipulation. These authentication protocols are better known as 'security packages'.

The SSPI API exposes these packages using a common API, and so a program may invoke one or the other with only minor changes in implementation. SSPI also supports the 'negotiate' 'meta' package, that allows a client and server to decide dynamically which real security provider to use, and then itself provides a passthrough interface to the real package.

## Usage ##

Typically, a client acquires some form of a credential, either from the currently logged on user's security context, by acquiring a username and password from the user, or by some other means. The server acquires a credential in a similar manner. Each uses their credentials to identify themselves to each other.

A client and a server each start with uninitialized security contexts. They exchange negotiation and authentication tokens to perform authentication, and if all succeeds, they create a shared security context in the form of a client's context and a server's context. The effectively shared context agrees on the security package to use (kerberos, NTLM), and what parameters to use for message passing. Every new client that authenticates with a server creates a new security context specific to that client-server pairing.

From the software perspective, a client security context initializes itself by exchanging authentication tokens with a server; the server initializes itself by exchanging authentication tokens with the client.

This API provides raw access to the authentication tokens created during the negotiation and authentication process. In this manner, any application can integrate SSPI-based authentication by deciding for themselves how to integrate the tokens into their application protocol.

The project is broken up into 3 chunks:

 * The NSspi library, which provides safe, managed access to the SSPI API.
 * NsspiDemo, a quick demo program to show how to exercise the features of NSspi locally.
 * UI demo programs TestClient and TestServer (that have a common dependency on TestProtocol) that
   may be run on separate machines, that show how one might integrate SSPI into a custom 
   application.

## More information ##

If you would like to understand the SSPI API, feel free to browse the following references:

MSDN documentation on the SSPI API:<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; [http://msdn.microsoft.com/en-us/library/windows/desktop/aa374731(v=vs.85).aspx](http://msdn.microsoft.com/en-us/library/windows/desktop/aa374731\(v=vs.85\).aspx)

MSDN article on SSPI along with a sample Managed C++ SSPI library and UI client/servers.<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; [http://msdn.microsoft.com/en-us/library/ms973911.aspx](http://msdn.microsoft.com/en-us/library/ms973911.aspx)

Relevant StackOverflow questions:

* [Client-server authentication - using SSPI?](http://stackoverflow.com/questions/17241365/)
* [Validate Windows Identity Token](http://stackoverflow.com/questions/11238141/)
* [How to deal with allocations in constrained execution regions?](http://stackoverflow.com/questions/24442209/)
* [AcquireCredentialsHandle returns massive expiration time](http://stackoverflow.com/questions/24478056/)
