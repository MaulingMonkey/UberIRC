﻿// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Text;
using Industry;

namespace UberIRC.NET {
	public struct IrcConnectionID {
		public string Hostname;
		public int?   Port;
		public bool   SSL;

		public static bool operator==( IrcConnectionID lhs, IrcConnectionID rhs ) {
			return lhs.Hostname == rhs.Hostname
				&& lhs.Port == rhs.Port
				&& lhs.SSL == rhs.SSL
				;
		}
		public static bool operator!=( IrcConnectionID lhs, IrcConnectionID rhs ) { return !(lhs==rhs); }

		public override bool Equals(object obj) {
			if ( obj.GetType() != typeof(IrcConnectionID) ) return base.Equals(obj);
			else return this == (IrcConnectionID)obj;
		}

		public override int GetHashCode() {
			unchecked {
				return Hostname.GetHashCode()
					+ Port.GetHashCode()
					+ SSL.GetHashCode()
					;
			}
		}
	}

	public struct IrcChannelID {
		public IrcConnection Connection;
		public String        Channel;
	}

	public struct IrcConnectUserParams {
		public String Nick;
		public String ID;
		public String Host;
		public String RealName;
	}
	public struct IrcConnectParams {
		public IrcConnectionID      To;
		public IrcConnectUserParams User;
		public String   Password;
		public Encoding Encoding;
		public String[] Channels;
	}

	public partial class IrcConnection : IDisposable {
		class User {
			string hostname = null;
			public string Hostname { get {
				return hostname;
			} set {
				hostname = value;
				if ( OnResolveHostname != null ) OnResolveHostname();
				OnResolveHostname = null;
			} }
			public event Action OnResolveHostname = null;
		}

		class Channel {
			public Dictionary<String,User> Users = new Dictionary<String,User>();
		}

		public void Dispose() {
			lock (Lock) {
				Send("QUIT");
				Client.Close();
				RAII.Dispose(this);
			}
		}
		
		IrcConnectParams Parameters;
		String           actualNickname;
		String           targetNickname;
		public String ActualNickname { get { return actualNickname; } private set { actualNickname = value; } }
		public String TargetNickname { get { return targetNickname; } private set { targetNickname = value; } }
		Encoding         Encoding;
		HashSet<String>  TargetChannels = new HashSet<String>();
		Dictionary<String,Channel> Channels = new Dictionary<String,Channel>();
		bool             AutoRejoin    = true;
		bool             AutoReconnect = true;

		Channel AddChannel( string id ) {
			if ( Channels.ContainsKey(id) ) {
				Channels[id].Users.Clear();
			} else {
				Channels.Add(id,new Channel());
			}
			return Channels[id];
		}

		void RemoveChannel( string id ) {
			if ( Channels.ContainsKey(id) ) Channels.Remove(id);
		}

		public IrcConnection( IrcConnectParams p ): this(p,new HashSet<IEventListener>()) {}
		public IrcConnection( IrcConnectParams p, HashSet<IEventListener> listeners ) {
			if ( p.Channels != null ) foreach ( string channel in p.Channels ) AddChannel(channel);
			Parameters     = p;
			TargetNickname = p.User.Nick;
			Encoding       = p.Encoding;
			Listeners = listeners;
			BeginReconnect();
		}

		public IrcConnectionID ConnectionID { get { return Parameters.To; } }

		public readonly HashSet<IEventListener> Listeners;

		public IEnumerable<String> WhosIn( string channel ) {
			if ( Channels.ContainsKey(channel) ) return Channels[channel].Users.Keys;
			else return new string[]{};
		}

		public void Join( string channel ) {
			lock (Lock) {
				if ( Registered ) Send( "JOIN "+channel );
				TargetChannels.Add(channel);
			}
		}

		public void Part( string channel ) {
			lock (Lock) {
				if ( Registered ) Send( "PART "+channel );
				TargetChannels.Remove(channel);
			}
		}

		public void Nick( string newnick ) {
			lock (Lock) {
				Send( "NICK "+newnick );
				TargetNickname = newnick;
			}
		}

		public void Topic( string channel, string topic ) {
			Send( "TOPIC "+channel+" :"+topic );
		}
		
		public void RequestTopic( string channel ) {
			Send( "TOPIC "+channel );
		}

		public void Kick( string channel, string target, string message ) {
			Send( "KICK "+channel+" "+target+((message.Length!=0)?(" :"+message):"") );
		}

		public void Ban( string channel, string target ) {
			KickBan(channel,target,null);
		}
		public void KickBan( string channel, string target, string message ) {
			// Will not kick if message == null

			lock (Lock)
			if ( Channels.ContainsKey(channel) )
			{
				var c = Channels[channel];
				
				if ( c.Users.ContainsKey(target) ) {
					var u = c.Users[target];
					if ( u.Hostname != null ) {
						Send( "MODE "+channel+" +b *!*@"+u.Hostname );
						if ( message != null ) Kick( channel, target, message );
					} else {
						u.OnResolveHostname += () => {
							Send( "MODE "+channel+" +b "+u.Hostname );
							if ( message != null ) Kick( channel, target, message );
						};
						Send("WHOIS "+target);
					}
				}
			}
			else
			{
				Send("MODE "+channel+" +b "+target);
				if ( message != null ) Kick( channel, target, message ); // could be a kick mask?
			}
		}

		public void ChangeModes( string channel, Irc.ModeChangeSet modes ) {
			foreach ( var mode in modes.AllModes ) {
				var _value = (mode.Value==null)?"":(" "+mode.Value);
				Send( "MODE "+channel+" "+mode.Key+_value );
			}
		}
	}
}
