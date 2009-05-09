// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Industry;

namespace UberIRC.NET {
	public partial class IrcConnection : IDisposable {
		object Lock = new object();
		[Owns] TcpClient     Client;

		class RecvState {
			public byte[]       Buffer;
			public int          Offset;
			public SocketError  Error;
		}

		void BeginRecv( byte[] buffer, int offset ) {
			lock (Lock) try {
				var state = new RecvState() { Buffer = buffer, Offset = offset };
				Client.Client.BeginReceive( buffer, offset, buffer.Length-offset, SocketFlags.Partial, out state.Error, OnRecv, state );
			} catch ( Exception e ) {
				Handle(e);
			}
		}

		public void Send( string s ) {
			lock (Lock) try {
				if ( Client == null ) return;
				if ( !Client.Connected ) return;
				var b = Parameters.Encoding.GetBytes(s+"\r\n");
				Client.Client.BeginSend( b, 0, b.Length, SocketFlags.None, OnSend, null );
			} catch ( Exception e ) {
				Handle(e);
			}
		}

		void OnSend( IAsyncResult result ) {
			lock (Lock) try {
				Client.Client.EndSend( result );
			} catch ( Exception e ) {
				Handle(e);
			}
		}

		void OnRecv( IAsyncResult result ) {
			lock (Lock) try {
				var state = (RecvState) result.AsyncState;
				var buffer = state.Buffer;
				if (Client == null || Client.Client == null || !Client.Connected) return; // XXX -- do we really want to silent-fail here?
				var buffer_end = Client.Client.EndReceive(result) + state.Offset;
				Handle(state.Error);
				var eol = Encoding.GetBytes("\n")[0]; // XXX

				var begin = 0;
				for (;;) {
					var end = Array.FindIndex( buffer, begin, buffer_end-begin, (byte b) => b == eol );

					if ( end == -1 ) {
						for ( int i = begin ; i < buffer_end ; ++i ) buffer[i-begin] = buffer[i];
						BeginRecv(buffer,buffer_end-begin);
						return;
					}

					var s = Encoding.GetString(buffer, begin, end-begin).TrimEnd( new[]{'\r','\n'} );

					if (OnRecieve != null) OnRecieve(s);
					Match match;
					if ( ( match = new Regex(@"^PING(.*)$").Match(s)).Success ) {
						var code = match.Groups[1].Value;
						Send( "PONG" + code );
					} else if ( (match = new Regex(@"^\:([^ ]+) (\d\d\d) \:?(.*)$").Match(s)).Success ) {
						var sender     = match.Groups[1].Value;
						var code       = match.Groups[2].Value;
						var parameters = match.Groups[3].Value;
						switch (code) {
						case "001": // Welcome
							Registered = true;
							foreach ( string channel in TargetChannels ) Send("JOIN "+(channel.StartsWith("#")?channel:"#"+channel));
							break;
						case "331": // RPL_NOTOPIC
						case "332": // RPL_TOPIC
							if ( (match = new Regex(@"^[^ ]+ (?'channel'[^ ]+) \:?(?'topic'.+)$").Match(parameters)).Success ) {
								var channel = match.Groups["channel"].Value;
								var topic   = match.Groups["topic"].Value;
								if ( OnTopic != null ) OnTopic( this, null, channel, topic );
							}
							break;
						case "353": // RPL_NAMREPLY (Names list)
							if ( (match = new Regex(@"^[^:]*?(?'channel'[^: ]+) \:(?'nicks'.+)$").Match(parameters)).Success )
							{
								var channelname = match.Groups["channel"].Value;
								if (!Channels.ContainsKey(channelname)) break;
								var channel     = Channels[channelname];
								var nicks       = match.Groups["nicks"].Value.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
								
								foreach ( string nick_ in nicks ) {
									string nick;
									switch ( nick_[0] ) {
									case '@':
									case '%':
									case '+':
										nick = nick_.Substring(1);
										break;
									default:
										nick = nick_;
										break;
									}
									
									channel.Names.Add(nick);
								}
							}
							break;
						case "366": // RPL_ENDOFNAMES (End of names list)
							break;
						case "433": // Nick already in use
							ActualNickname = ActualNickname + "_";
							Send( "NICK "+ActualNickname); // try a different one
							break;
						}
					} else if ( (match = new Regex(@"^\:?(?'nick'[^ !]+)!(?'user'[^ @]+)@(?'host'[^ ]+) (?'action'[^ ]+)(?: (?'params'.+))?$").Match(s)).Success ) {
						var nick   = match.Groups["nick"].Value;
						var user   = match.Groups["user"].Value;
						var host   = match.Groups["host"].Value;
						var actor  = new Irc.Actor() { Nickname = nick, Username = user, Hostname = host };
						var action = match.Groups["action"].Value;
						var param  = match.Groups["params"].Value;

						switch ( action ) {
							case "NICK": {
								var newnick = ReadParam(ref param);
								if ( nick == ActualNickname ) ActualNickname = newnick; // we got renamed!

								foreach ( var channel in Channels )
								if ( channel.Value.Names.Contains(nick) )
								{
									channel.Value.Names.Remove(nick);
									channel.Value.Names.Add(newnick);
									if ( OnNick != null ) OnNick( this, actor, channel.Key, newnick );
								}
								break;
							} case "JOIN": {
								var channel = ReadParam(ref param);
								if ( nick == ActualNickname ) AddChannel(channel); // we joined a channel!
								if ( Channels.ContainsKey(channel) ) Channels[channel].Names.Add(nick);
								if ( OnJoin != null ) OnJoin( this, actor, channel );
								break;
							} case "PART": {
								var channel = ReadParam(ref param);
								if ( nick == ActualNickname ) RemoveChannel(channel); // we left a channel!
								if ( Channels.ContainsKey(channel) ) Channels[channel].Names.Remove(nick);
								if ( OnPart != null ) OnPart( this, actor, channel );
								break;
							} case "QUIT": {
								var message = TrimColon(param);

								if ( OnQuit != null )
								foreach ( var channel in Channels )
								if ( channel.Value.Names.Contains(nick) )
								{
									OnQuit( this, actor, channel.Key, message );
								}
								break;
							} case "KICK": {
								var channel = ReadParam(ref param);
								var target  = ReadParam(ref param);
								var message = TrimColon(param);
								if ( Channels.ContainsKey(channel) ) Channels[channel].Names.Remove(target);
								if ( target == ActualNickname ) { // we were kicked from a channel!
									RemoveChannel( channel );
									if ( AutoRejoin ) Join( channel );
								}
								if ( OnKick != null ) OnKick( this, actor, channel, target, message );
								break;
							} case "PRIVMSG": {
								var target = ReadParam(ref param);
								var message = TrimColon(param);
								switch ( message ) {
								case "\u0001VERSION\u0001": // http://www.irchelp.org/irchelp/rfc/ctcpspec.html
									if ( target != ActualNickname ) break;
									Send( "NOTICE "+nick+" :\u0001VERSION UberIRC "+Assembly.GetExecutingAssembly().ImageRuntimeVersion+" Unknown Unavailable\u0001" );
									break;
								default:
									if ( OnPrivMsg != null ) OnPrivMsg( this, actor, target, message );
									break;
								}
								break;
							} case "MODE": try {
								var channel = ReadParam(ref param);
								var modes = new Irc.ModeChangeSet(param);
								
								if ( OnMode != null ) foreach ( var mode in modes.UserModes ) {
									OnMode( this, actor, channel, mode.Key, mode.Value );
								}
								if ( OnChannelMode != null ) foreach ( var mode in modes.ChannelModes ) {
									OnChannelMode( this, actor, channel, mode.Key, mode.Value );
								}
								break;
							} catch ( IndexOutOfRangeException ) {
								// squelch IOOREs from running out of parameters or otherwise having malformed MODEs
								break;
							} case "TOPIC": {
								var channel = ReadParam(ref param);
								var newtopic = TrimColon(param);
								if ( OnTopic != null ) OnTopic( this, actor, channel, newtopic );
								break;
							}
						}
					}

					begin = end+1;
				}
			}
			catch ( SocketException e ) { Handle(e); }
			catch ( ObjectDisposedException e ) { Handle(e); }
		}
		public static string ReadParam( ref string input ) {
			string result;
			int space = input.IndexOf(' ');
			if ( space == -1 ) {
				result = TrimColon(input);
				input  = "";
			} else {
				result = TrimColon(input.Substring(0,space));
				input  = input.Substring(space+1);
			}
			return result;
		}
		public static string TrimColon( string input ) {
			if (input[0] != ':') return input;
			return input.Substring(1);
		}
	}
}
