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

					//if (OnRecieve != null) OnRecieve(s);
					Match match;
					try {
						if ( ( match = new Regex(@"^PING(.*)$").Match(s)).Success ) {
							var code = match.Groups[1].Value;
							Send( "PONG" + code );
						} else if ( (match = new Regex(@"^\:([^ ]+) (\d\d\d) ([^: ]+) \:?(.*)$").Match(s)).Success ) {
							var sender     = match.Groups[1].Value;
							var code       = match.Groups[2].Value;
							var target     = match.Groups[3].Value;
							var parameters = match.Groups[4].Value;
							if (serverIdent == null) serverIdent = sender;

							if ( target != "*" && code != "433" ) ActualNickname=target; // Perhaps we should do this only on code=="001" (welcome) instead?  We seem to get confirmation NICKs from the server for everything but the initial login NICK
							
							switch (code) {
							case "001": // Welcome
								Registered = true;
								foreach ( string channel in TargetChannels ) Send("JOIN "+(channel.StartsWith("#")?channel:"#"+channel));
								break;
							case "311": // RPL_WHOISUSER "<nick> <user> <host> * :<real name>"
							case "314": // RPL_WHOWASUSER "<nick> <user> <host> * :<real name>"
								if ( (match = new Regex(@"^([^ ]+) ([^ ]+) ([^ ]+) \* \:(.+)$").Match(parameters)).Success ) {
									var nick = match.Groups[1].Value;
									var user = match.Groups[2].Value;
									var host = match.Groups[3].Value;
									var real = match.Groups[4].Value;

									if ( Users.ContainsKey(nick) ) Users[nick].Hostname = host;
								}
								break;
							case "331": // RPL_NOTOPIC
							case "332": // RPL_TOPIC
								if ( (match = new Regex(@"^(?'channel'[^ ]+) \:?(?'topic'.+)$").Match(parameters)).Success ) {
									var channel = match.Groups["channel"].Value;
									var topic   = match.Groups["topic"].Value;
									foreach ( var l in Listeners ) l.OnTopic( this, null, channel, topic );
								}
								break;
							case "341": // RPL_INVITING
								if ( (match = new Regex(@"^(?'nick'[^ ]+) (?'channel'.+)$").Match(parameters)).Success ) {
									var nick    = match.Groups["nick"   ].Value;
									var channel = match.Groups["channel"].Value;
									foreach ( var l in Listeners ) l.OnRplInvited( this, new Irc.Actor() { Nickname = nick, Hostname = "???", Username = "???" }, channel );
								}
								break;
							case "353": // RPL_NAMREPLY (Names list)
								if ( (match = new Regex(@"^(?:. )?(?'channel'[^: ]+) \:(?'nicks'.+)$").Match(parameters)).Success )
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
										
										channel.Users.Add( nick );
										if (!Users.ContainsKey(nick)) Users.Add(nick,new User());
									}
								}
								break;
							case "366": // RPL_ENDOFNAMES (End of names list)
								break;
							case "401": // RPL_NOSUCHNICK "<nickname> :No such nick/channel"
								Send( "WHOWAS "+ReadParam(ref parameters) );
								break;
							case "406": { // RPL_WASNOSUCHNICK "<nickname> :There was no such nickname"
								var nick = ReadParam(ref parameters);
								if ( Users.ContainsKey(nick) ) Users.Remove(nick);
								break;
							} case "433": // ERR_NICKNAMEINUSE Nick already in use
								if ( ActualNickname == null ) Send( "NICK "+(LastTriedNickname=LastTriedNickname+"_")); // try a different one
								else foreach ( var l in Listeners ) l.OnErrNickInUse( this, TargetNickname );
								break;
							case "482": // ERR_CHANOPRIVSNEEDED "<channel> :You're not channel operator"
								if ( (match = new Regex(@"^(?'channel'[^ ]+) \:?(?'message'.+)$").Match(parameters)).Success ) {
									var channel = match.Groups["channel"].Value;
									var message = match.Groups["message"].Value;
									foreach ( var l in Listeners ) l.OnErrNotChannelOp( this, channel, message );
								}
								break;
							default:
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
									if ( Users.ContainsKey(nick) ) {
										var info = Users[nick];
										if ( !Users.ContainsKey(newnick) ) Users.Add(newnick,info);
										Users.Remove(nick);
									}

									foreach ( var channel in Channels )
									if ( channel.Value.Users.Contains(nick) )
									{
										channel.Value.Users.Remove(nick);
										channel.Value.Users.Add(newnick);
										foreach ( var l in Listeners ) l.OnNick( this, actor, channel.Key, newnick );
									}
									break;
								} case "JOIN": {
									var channel = ReadParam(ref param);
									if ( nick == ActualNickname ) AddChannel(channel); // we joined a channel!
									if ( Channels.ContainsKey(channel) ) Channels[channel].Users.Add(nick);
									foreach ( var l in Listeners ) l.OnJoin( this, actor, channel );
									break;
								} case "PART": {
									var channel = ReadParam(ref param);
									if ( nick == ActualNickname ) RemoveChannel(channel); // we left a channel!
									if ( Channels.ContainsKey(channel) ) Channels[channel].Users.Remove(nick);
									foreach ( var l in Listeners ) l.OnPart( this, actor, channel );
									break;
								} case "QUIT": {
									var message = TrimColon(param);

									foreach ( var l in Listeners )
									foreach ( var channel in Channels )
									if ( channel.Value.Users.Contains(nick) )
									{
										channel.Value.Users.Remove(nick);
										l.OnQuit( this, actor, channel.Key, message );
									}

									if ( nick == TargetNickname ) Nick(TargetNickname);

									break;
								} case "KICK": {
									var channel = ReadParam(ref param);
									var target  = ReadParam(ref param);
									var message = TrimColon(param);
									if ( Channels.ContainsKey(channel) ) Channels[channel].Users.Remove(target);
									if ( target == ActualNickname ) { // we were kicked from a channel!
										RemoveChannel( channel );
										if ( AutoRejoin ) Join( channel );
									}
									foreach ( var l in Listeners ) l.OnKick( this, actor, channel, target, message );
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
										foreach ( var l in Listeners ) l.OnPrivMsg( this, actor, target, message );
										break;
									}
									break;
								} case "NOTICE": {
									var target = ReadParam(ref param);
									var message = TrimColon(param);
									foreach ( var l in Listeners ) l.OnNotice( this, actor, target, message );
									break;
								} case "MODE": {
									var channel = ReadParam(ref param);
									var modes = new Irc.ModeChangeSet(param);
									
									foreach ( var l in Listeners ) {
										foreach ( var mode in modes.UserModes    ) l.OnModeChange( this, actor, channel, mode.Key, mode.Value );
										foreach ( var mode in modes.ChannelModes ) l.OnChannelModeChange( this, actor, channel, mode.Key, mode.Value );
									}
									break;
								} case "TOPIC": {
									var channel = ReadParam(ref param);
									var newtopic = TrimColon(param);
									foreach ( var l in Listeners ) l.OnTopic( this, actor, channel, newtopic );
									break;
								}
							}
						}
					} catch ( Exception e ) {
						foreach ( var l in Listeners ) l.OnRecvParseError( this, s, e );
					}

					begin = end+1;
				}
			}
			catch ( SocketException e ) { Handle(e); }
			catch ( ObjectDisposedException e ) { Handle(e); }
			catch ( Exception e ) { foreach ( var l in Listeners ) l.OnConnectionError(this,e); }
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
