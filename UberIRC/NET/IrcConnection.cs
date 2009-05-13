// Copyright Michael B. E. Rickert 2009
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
		class Channel {
			public HashSet<String> Names = new HashSet<string>();
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
				Channels[id].Names.Clear();
			} else {
				Channels.Add(id,new Channel());
			}
			return Channels[id];
		}

		void RemoveChannel( string id ) {
			if ( Channels.ContainsKey(id) ) Channels.Remove(id);
		}

		public IrcConnection( IrcConnectParams p ) {
			if ( p.Channels != null ) foreach ( string channel in p.Channels ) AddChannel(channel);
			Parameters     = p;
			TargetNickname = p.User.Nick;
			Encoding       = p.Encoding;
			BeginReconnect();
		}

		public IrcConnectionID ConnectionID { get { return Parameters.To; } }

		public delegate void OnRecieveHandler( String message );
		public event OnRecieveHandler OnRecieve;

		public event Irc.NickEvent OnNick;
		public event Irc.JoinEvent OnJoin;
		public event Irc.PartEvent OnPart;
		public event Irc.QuitEvent OnQuit;
		public event Irc.KickEvent OnKick;
		public event Irc.ModeEvent OnMode;
		public event Irc.PrivMsgEvent OnPrivMsg;
		public event Irc.ChannelModeEvent OnChannelMode;
		public event Irc.TopicEvent OnTopic;

		public IEnumerable<String> WhosIn( string channel ) {
			if ( Channels.ContainsKey(channel) ) return Channels[channel].Names;
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

		public void Kick( string channel, string target, string message ) {
			Send( "KICK "+channel+" "+target+((message.Length!=0)?(" :"+message):"") );
		}

		public void ChangeModes( string channel, Irc.ModeChangeSet modes ) {
			foreach ( var mode in modes.AllModes ) {
				var _value = (mode.Value==null)?"":(" "+mode.Value);
				Send( "MODE "+channel+" "+mode.Key+_value );
			}
		}
	}
}
