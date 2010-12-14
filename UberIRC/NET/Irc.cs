// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Industry;

namespace UberIRC.NET {
	public partial class Irc : RAII {
		public readonly HashSet<IEventListener> Listeners = new HashSet<IEventListener>();

		public static string NicknameOf( string nickidhost ) { return nickidhost.Substring(0,nickidhost.IndexOf('!')); }

		[Owns(false,true)] Dictionary< IrcConnectionID, IrcConnection > Connections = new Dictionary<IrcConnectionID,IrcConnection>();

		Settings settings;

		IrcConnectionID UriToConnectionId( Uri uri ) {
			return new IrcConnectionID()
				{ Hostname = uri.Host
				, Port = uri.Port==-1 ? 6667 : uri.Port
				, SSL = uri.Scheme == "ircs"
				};
		}

		public Irc( Settings settings ) { this.settings = settings; }
		public IrcConnection Connect( string url ) { return Connect( new Uri(url) ); }
		public IrcConnection Connect( Uri uri ) { return Connect( UriToConnectionId(uri) ); }
		public IrcConnection Connect( IrcConnectionID id ) {
			var server = this.settings.Servers.FirstOrDefault( (s) => UriToConnectionId(s.Uri) == id ) ?? this.settings.DefaultServerSettings;

			var p = new IrcConnectParams()
				{ To   = id
				, User =
					{ Host     = server.Userhost
					, RealName = server.Realname
					, ID       = server.Username
					, Nick     = server.Nickname
					}
				, Encoding = Encoding.UTF8
				, Password = server.Password
				, Channels = server.Channels.Select( (s) => s.Name ).ToArray()
				};
			
			IrcConnection connection;
			if ( !Connections.ContainsKey(id) ) {
				connection = new IrcConnection(p,Listeners);
				Connections.Add(id,connection);
			} else {
				connection = Connections[id];
			}
			return connection;
		}

		public IrcChannelID? Join( string url ) {
			return Join( new Uri(url) );
		}
		public IrcChannelID? Join( Uri uri ) {
			var chan  = uri.PathAndQuery.Length > 1 ? "#"+uri.PathAndQuery.Substring(1) : null;

			IrcConnection connection = Connect(uri);
			if ( chan != null ) {
				connection.Join( chan );
				return new IrcChannelID() { Connection = connection, Channel = chan };
			} else {
				return null;
			}
		}

		public void Part( string url ) {
			var uri = new UriBuilder(url);
			if ( uri.Path.Length <= 1 ) throw new UriFormatException( "Expected a URI with a channel" );

			var connid = new IrcConnectionID()
				{ Hostname = uri.Host
				, Port     = uri.Port == -1 ? (int?)null : uri.Port
				, SSL      = uri.Scheme == "ircs"
				};
			var chan = "#"+uri.Path.Substring(1);
			if ( !Connections.ContainsKey(connid) ) throw new Exception( "Not connected to " + url );

			Connections[connid].Part(chan);
		}

		public class Actor {
			public string Nickname;
			public string Username;
			public string Hostname;
			public override string ToString() { return Nickname + "!" + Username + "@" + Hostname; }
		}
	}
}
