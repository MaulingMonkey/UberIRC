// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
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
		public CIIrcName     Channel;
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

		public class ChannelUserInfo {
			public string Sigil = " ";
			public string Nick  = null;

			public ChannelUserInfo( string sigil, string nick )
			{
				Sigil	= sigil;
				Nick	= nick;
			}
		}

		class Channel {
			public readonly Dictionary<String,ChannelUserInfo> Users = new Dictionary<String,ChannelUserInfo>();
		}

		public void Dispose() {
			lock (Lock) {
				Send("QUIT");
				Client.Close();
				RAII.Dispose(this);
			}
		}
		
		IrcConnectParams Parameters;
		String           serverIdent;
		public CIIrcName ActualNickname    { get; private set; }
		public CIIrcName LastTriedNickname { get; private set; } // Only used in initial auto-nick selection
		public CIIrcName TargetNickname    { get; private set; }
		Encoding         Encoding;
		bool             AutoRejoin    = true;
		bool             AutoReconnect = true;
		readonly HashSet<String>               TargetChannels = new HashSet<String>();
		readonly Dictionary<CIIrcName,Channel> Channels = new Dictionary<CIIrcName,Channel>();
		readonly Dictionary<CIIrcName,User>    Users    = new Dictionary<CIIrcName,User>();
		[Owns] Timer     HeartbeatTimer = new Timer() { AutoReset = true, Interval = 16000.0f, Enabled = true };
		
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
			Listeners      = listeners;
			BeginReconnect();
			HeartbeatTimer.Elapsed += new ElapsedEventHandler(HeartbeatTimer_Elapsed);
		}

		void HeartbeatTimer_Elapsed(object sender, ElapsedEventArgs e) {
			lock (Lock) {
				if ( Client.Connected ) {
					if (serverIdent!=null) Send("PING "+serverIdent);
				} else {
					BeginReconnect();
				}
			}
		}

		public IrcConnectionID ConnectionID { get { return Parameters.To; } }

		public readonly HashSet<IEventListener> Listeners;

		public IEnumerable<ChannelUserInfo> WhosIn( string channel ) {
			lock (Lock) {
				if ( Channels.ContainsKey(channel) ) return Channels[channel].Users.Values.ToArray();
				else return new ChannelUserInfo[]{};
			}
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
			if ( target.Contains("!") || target.Contains("@" ) ) { // hostname
				DoKickBan( channel, null, null, target ); // TODO:  Whine at the user if message!=null that we can only ban, sorry cupcake
			} else { // nickname
				if (!Users.ContainsKey(target)) Users.Add(target, new User());
				var u = Users[target];
				u.OnResolveHostname += () => DoKickBan( channel, target, message, "*!*@"+u.Hostname );
				Send("WHOIS "+target);
			}
		}
		private void DoKickBan( string channel, string target, string message, string hostmask ) {
			Send( "MODE "+channel+" +b "+hostmask );
			if ( message != null ) Kick( channel, target, message );
		}
		public void UnBan( string channel, string target ) {
			lock (Lock)
			if ( target.Contains("!") || target.Contains("@" ) ) { // hostname
				Send( "MODE "+channel+" -b "+target );
			} else { // nickname
				if (!Users.ContainsKey(target)) Users.Add(target, new User());
				var u = Users[target];
				u.OnResolveHostname += () => Send( "MODE "+channel+" -b *!*@"+u.Hostname );
				Send("WHOIS "+target);
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
