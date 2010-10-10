// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Net.Sockets;

namespace UberIRC.NET {
	public partial class IrcConnection : IDisposable {
		bool Registered = false;

		public void BeginReconnect() {
			lock (Lock) try {
				Registered = false;
				var p = Parameters;

				Client = new TcpClient();
				Client.BeginConnect( p.To.Hostname, p.To.Port ?? (p.To.SSL ? 9998 : 6667), new AsyncCallback(OnReconnect), null );
				Channels.Clear(); //foreach ( var channel in Channels.Values ) channel.Names.Clear();
			} catch( Exception e ) {
				Handle(e);
			}
		}

		void OnReconnect( IAsyncResult result ) {
			lock (Lock) try {
				var p = Parameters;

				Client.EndConnect(result);
				//Stream = Client.GetStream();

				ActualNickname = null;
				if (p.Password!=null) Send( "PASS "+p.Password );
				Send( "USER "+p.User.ID+" "+p.User.Host+" "+p.User.Host+" "+p.User.RealName );
				Send( "NICK "+(LastTriedNickname=TargetNickname) );
				BeginRecv( new byte[4096], 0 );
			} catch( Exception e ) {
				Handle(e);
			}
		}
	}
}
