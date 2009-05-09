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
			var server = this.settings.Servers.First( (s) => UriToConnectionId(s.Uri) == id );
			
			var p = new IrcConnectParams()
				{ To = id
				, User =
					{ Host = server.Userhost
					, RealName = server.Realname
					, ID = server.Username
					, Nick = server.Nickname
					}
				, Encoding = Encoding.UTF8
				, Password = server.Password
				, Channels = server.Channels.Select( (s) => s.Name ).ToArray()
				};
			
			IrcConnection connection;
			if ( !Connections.ContainsKey(id) ) {
				connection = new IrcConnection(p);
				connection.OnNick += OnNick;
				connection.OnJoin += OnJoin;
				connection.OnPart += OnPart;
				connection.OnQuit += OnQuit;
				connection.OnKick += OnKick;
				connection.OnMode += OnMode;
				connection.OnPrivMsg += OnPrivMsg;
				connection.OnChannelMode += OnChannelMode;
				connection.OnTopic += OnTopic;
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
		public delegate void NickEvent( IrcConnection connection, Actor who, string channel, string new_ );
		public delegate void JoinEvent( IrcConnection connection, Actor who, string channel );
		public delegate void PartEvent( IrcConnection connection, Actor who, string channel );
		public delegate void QuitEvent( IrcConnection connection, Actor who, string channel, string message );
		public delegate void KickEvent( IrcConnection connection, Actor op , string channel, string target, string message );
		public delegate void ModeEvent( IrcConnection connection, Actor op , string channel, string mode, string target );
		public delegate void PrivMsgEvent( IrcConnection connection, Actor who, string target, string message );
		public delegate void ChannelModeEvent( IrcConnection connection, Actor op, string channel, string mode, string param );
		public delegate void TopicEvent( IrcConnection connection, Actor who, string channel, string topic );

		public event NickEvent OnNick;
		public event JoinEvent OnJoin;
		public event PartEvent OnPart;
		public event QuitEvent OnQuit;
		public event KickEvent OnKick;
		public event ModeEvent OnMode;
		public event PrivMsgEvent OnPrivMsg;
		public event ChannelModeEvent OnChannelMode;
		public event TopicEvent OnTopic;
	}
}
