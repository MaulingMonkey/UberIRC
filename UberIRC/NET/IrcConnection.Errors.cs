// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Net.Sockets;

namespace UberIRC.NET {
	public partial class IrcConnection : IDisposable {
		public delegate void OnSocketErrorHandler( SocketError error );
		public event OnSocketErrorHandler OnSocketError;

		void Handle( Exception e ) {
			try {
				foreach ( var l in Listeners ) l.OnConnectionError(this,e);
			} catch ( SocketException se ) {
				Handle(se.SocketErrorCode);
			} catch ( ObjectDisposedException ) {
				Handle(SocketError.Disconnecting);
			}
		}
		void Handle( SocketError se ) {
			switch ( se ) {
			case SocketError.Success:
				return; // not an error
			case SocketError.ConnectionReset:
				if (AutoReconnect) BeginReconnect();
				break;
			}
			if ( OnSocketError != null ) OnSocketError(se);
		}
	}
}
