// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Industry;
using UberIRC.NET;

namespace UberIRC {
	public partial class IrcView {
		[Owns] Irc irc;

		void InitializeIO() {
			irc.OnChannelMode += (conn,op,ch,mode,param)     => BeginInvoke( new Action( () => irc_OnChannelMode(conn,op,ch,mode,param) ) );
			irc.OnJoin        += (conn,who,ch)               => BeginInvoke( new Action( () => irc_OnJoin       (conn,who,ch) ) );
			irc.OnKick        += (conn,op,ch,target,message) => BeginInvoke( new Action( () => irc_OnKick       (conn,op,ch,target,message) ) );
			irc.OnMode        += (conn,op,ch,mode,target)    => BeginInvoke( new Action( () => irc_OnMode       (conn,op,ch,mode,target) ) );
			irc.OnNick        += (conn,who,ch,new_)          => BeginInvoke( new Action( () => irc_OnNick       (conn,who,ch,new_) ) );
			irc.OnPart        += (conn,who,chan)             => BeginInvoke( new Action( () => irc_OnPart       (conn,who,chan) ) );
			irc.OnPrivMsg     += (conn,who,target,message)   => BeginInvoke( new Action( () => irc_OnPrivMsg    (conn,who,target,message) ) );
			irc.OnQuit        += (conn,who,ch,message)       => BeginInvoke( new Action( () => irc_OnQuit       (conn,who,ch,message) ) );
			irc.OnTopic       += (conn,who,ch,topic)         => BeginInvoke( new Action( () => irc_OnTopic      (conn,who,ch,topic) ) );
		}
		
		void OnEnter() {
			if ( CurrentView == null ) return;

			var torun = CurrentView.Input.Text;
			if ( torun.Length == 0 ) return; // don't send empty messages
			CurrentView.Input.Text = "";

			foreach ( var line in torun.Split( new[] { "\r\n", "\n\r", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries ) )
			if ( line.StartsWith("//") )
			{
				// shorthand command for "/say /"
				SendMessage(line.Substring(1));
			}
			else if ( line.StartsWith("/") )
			{
				// command
				var space = line.IndexOf(' ');
				var command    = (space!=-1) ? line.Substring(1,space-1) : line.Substring(1);
				var parameters = (space!=-1) ? line.Substring(space+1)   : "";

				if ( Commands.ContainsKey(command) ) {
					Commands[command]( parameters );
				} else {
					CurrentView.History.Add( new HistoryEntry()
						{ Nickname  = "ERROR"
						, Timestamp = Timestamp
						, Message   = "Unrecognized command "+command
						, Style     = commanderror
						});
				}
			}
			else
			{
				// not a command, just text
				SendMessage(line);
			}
		}

		void Join( string url ) {
			if ( url.Contains("/") ) {
				irc.Join(url);
			} else if ( CurrentView != null ) foreach ( var chan in url.Split(',') ) {
				CurrentView.ID.Connection.Join(chan);
			} else {
				// lol we can't do anything
			}
		}

		void Part( string url ) {
			if ( url.Contains("/") ) {
				irc.Part(url);
			} else if ( CurrentView != null ) foreach ( var chan in url.Split(',') ) {
				CurrentView.ID.Connection.Part(chan);
			} else {
				// lol we can't do anything
			}
		}

		void ChangeNick( string newnick ) {
			if ( newnick == "" ) return;
			CurrentView.ID.Connection.Nick(newnick);
		}

		void Topic( string topic ) {
			if ( topic != "" ) CurrentView.ID.Connection.Topic( CurrentView.ID.Channel, topic );
			else CurrentView.ID.Connection.RequestTopic(CurrentView.ID.Channel);
		}

		void SendMessage( string line ) {
			irc_OnPrivMsg( CurrentView.ID.Connection, new Irc.Actor() { Nickname = CurrentView.ID.Connection.ActualNickname, Hostname = "???", Username = "???" }, CurrentView.ID.Channel, line );
			CurrentView.ID.Connection.Send( "PRIVMSG "+CurrentView.ID.Channel+" :"+line );
		}

		void SendAction( string line ) {
			line = "\u0001ACTION "+line+"\u0001";
			SendMessage(line);
		}

		public void BeginTrySendMessage( string line ) {
			BeginInvoke( new Action( () => { if ( CurrentView!=null ) SendMessage(line); } ) );
		}
		public void BeginTrySendAction( string line ) {
			BeginInvoke( new Action( () => { if ( CurrentView!=null ) SendAction(line); } ) );
		}

		void irc_OnChannelMode(IrcConnection connection, Irc.Actor op, string channel, string mode, string param) {
			var view = ViewOf(connection,channel);
			if ( view==null ) return;

			view.History.Add( new HistoryEntry()
				{ Nickname  = ""
				, Timestamp = Timestamp
				, Message   = op.Nickname + " has set mode " + mode + " " + param
				, Style     = system
				});
			if ( view == CurrentView ) Invalidate();
		}

		void irc_OnJoin(IrcConnection connection, Irc.Actor who, string channel) {
			var view = ViewOf(connection,channel);
			if ( view==null ) return;

			view.History.Add( new HistoryEntry()
				{ Nickname  = ""
				, Timestamp = Timestamp
				, Message   = who + " has joined the channel"
				, Style     = system
				});
			if ( view == CurrentView ) Invalidate();
		}

		void irc_OnKick(IrcConnection connection, Irc.Actor op, string channel, string target, string message) {
			var view = ViewOf(connection,channel);
			if ( view==null ) return;

			view.History.Add( new HistoryEntry()
				{ Nickname  = ""
				, Timestamp = Timestamp
				, Message   = op.Nickname + " has kicked " + target + " from the channel"
				, Style     = system
				});
			if ( view == CurrentView ) Invalidate();
		}

		void irc_OnMode(IrcConnection connection, Irc.Actor op, string channel, string mode, string target) {
			var view = ViewOf(connection,channel);
			if ( view==null ) return;

			view.History.Add( new HistoryEntry()
				{ Nickname  = ""
				, Timestamp = Timestamp
				, Message   = op.Nickname + " has set mode " + mode + " on " + target
				, Style     = system
				});
			if ( view == CurrentView ) Invalidate();
		}

		void irc_OnNick(IrcConnection connection, Irc.Actor who, string channel, string new_) {
			var view = ViewOf(connection,channel);
			if ( view==null ) return;

			view.History.Add( new HistoryEntry()
				{ Nickname  = ""
				, Timestamp = Timestamp
				, Message   = who.Nickname + " is now known as " + new_
				, Style     = system
				});
			Invalidate();
		}

		void irc_OnPart(IrcConnection connection, Irc.Actor who, string channel) {
			var view = ViewOf(connection,channel);
			if ( view==null ) return;

			view.History.Add( new HistoryEntry()
				{ Nickname = ""
				, Timestamp = Timestamp
				, Message = who + " has left the channel"
				, Style = system
				});
			if ( view == CurrentView ) Invalidate();
		}

		void irc_OnPrivMsg(IrcConnection connection, Irc.Actor who, string target, string message) {
			var view = ViewOf(connection,target);
			if ( view==null ) return;

			var style
				= connection.ActualNickname == who.Nickname     ? self
				: message.Contains( connection.ActualNickname ) ? alerted
				: normal
				;

			Match m;
			if ( (m=new Regex("\u0001ACTION (?'action'.+)\u0001").Match(message)).Success ) {
				view.History.Add( new HistoryEntry()
					{ Nickname  = who.Nickname
					, Timestamp = Timestamp
					, Message   = m.Groups["action"].Value
					, Style     = style
					});
			} else {
				view.History.Add( new HistoryEntry()
					{ Nickname  = "<"+who.Nickname+">"
					, Timestamp = Timestamp
					, Message   = message
					, Style     = style
					});
			}
            if ( view == CurrentView ) Invalidate();
		}

		void irc_OnQuit(IrcConnection connection, Irc.Actor who, string channel, string message) {
			var view = ViewOf(connection,channel);
			if ( view==null ) return;

			view.History.Add( new HistoryEntry()
				{ Nickname = ""
				, Timestamp = Timestamp
				, Message = who + " has quit " + connection.ConnectionID.Hostname + (message=="" ? "" : (" ("+message+")"))
				, Style = system
				});
			Invalidate();
		}

		void irc_OnTopic( IrcConnection connection, Irc.Actor who, string channel, string topic ) {
			var view = ViewOf(connection,channel);
			if ( view==null ) return;

			if ( who == null ) {
				view.History.Add( new HistoryEntry()
					{ Nickname = "TOPIC"
					, Timestamp = Timestamp
					, Message = topic
					, Style = system
					});
			} else {
				view.History.Add( new HistoryEntry()
					{ Nickname = ""
					, Timestamp = Timestamp
					, Message = who.Nickname + " has changed the topic to " + topic
					, Style = system
					});
			}
			Invalidate();
		}

		Channel ViewOf( IrcConnection connection, string channel ) {
			var id = new IrcChannelID() { Connection = connection, Channel = channel };
			if (!Views.ContainsKey(id)) CreateChannel(id);
			return Views[id];
		}

		IEnumerable<Channel> AllViews { get { lock ( Views ) foreach ( var view in Views.Values ) yield return view; } }

		string Timestamp { get { return DateTime.Now.ToString("[HH:mm]"); } }
	}
}
