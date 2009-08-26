// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Industry;
using Industry.FX;
using UberIRC.NET;
using System.Web;
using System.Diagnostics;

namespace UberIRC {
	public partial class IrcView : NET.IEventListener {
		[Owns] Irc irc;

		void Begin( Action a ) { BeginInvoke(a); }

		void AddHistory( Channel view, string nickname, string timestamp, string message, TextStyle style ) {
			view.History.Add( new HistoryEntry()
				{ Nickname  = nickname
				, Timestamp = timestamp
				, Message   = new[] { new TextRun() { Font = style.Message.Font, Text = message } }
				, Style     = style
				});
		}

		static readonly Regex reUrlPatterns = new Regex(@"\b(?:([^\s]+?:\/\/[^\s]+?)|([^\s]+?\.(?:com|net|org|edu|gov|mil|info|biz)[^\s]*?)|(www\.[^\s]+?))(?=\s|$)");

		IEnumerable<TextRun> ToPrettyRuns( string message, TextStyle style ) {
			int lasturlend = 0;

			foreach ( Match match in reUrlPatterns.Matches(message) ) {
				if ( lasturlend != match.Index ) yield return new TextRun() { Font = style.Message.Font, Text = message.Substring(lasturlend,match.Index-lasturlend) };
				var command = new Action(() => {
					Process process = new Process();
					process.StartInfo.FileName = "rundll32.exe";
					process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + match.Value;
					process.StartInfo.UseShellExecute = true;
					process.Start();
				});
				yield return new TextRun() { Font = style.Message.LinkFont, Text = match.Value, Tag = command };
				lasturlend = match.Index + match.Length;
			}
			if ( lasturlend != message.Length ) yield return new TextRun() { Font = style.Message.Font, Text = message.Substring(lasturlend) };
		}

		void AddPrettyHistory( Channel view, string nickname, string timestamp, string message, TextStyle style ) {
			view.History.Add( new HistoryEntry()
				{ Nickname  = nickname
				, Timestamp = timestamp
				, Message   = ToPrettyRuns(message,style).ToArray()
				, Style     = style
				});
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
					AddHistory( CurrentView, "ERROR", Timestamp, "Unrecognized command "+command, commanderror );
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
			OnPrivMsg( CurrentView.ID.Connection, new Irc.Actor() { Nickname = CurrentView.ID.Connection.ActualNickname, Hostname = "???", Username = "???" }, CurrentView.ID.Channel, line );
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

		public void OnChannelModeChange(IrcConnection connection, Irc.Actor op, string channel, string mode, string param) {
			Begin(()=>{
				var view = ViewOf(connection,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, op.Nickname + " has set mode " + mode + " " + param, system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnJoin(IrcConnection connection, Irc.Actor who, string channel) {
			Begin(()=>{
				var view = ViewOf(connection,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, who + " has joined the channel", system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnKick(IrcConnection connection, Irc.Actor op, string channel, string target, string message) {
			Begin(()=>{
				var view = ViewOf(connection,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, op.Nickname + " has kicked " + target + " from the channel" + (message=="" ? "" : (" ("+message+")")), system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnModeChange(IrcConnection connection, Irc.Actor op, string channel, string mode, string target) {
			Begin(()=>{
				var view = ViewOf(connection,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, op.Nickname + " has set mode " + mode + " on " + target, system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnNick(IrcConnection connection, Irc.Actor who, string channel, string new_) {
			Begin(()=>{
				var view = ViewOf(connection,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, who.Nickname + " is now known as " + new_, system );
				Invalidate();
			});
		}

		public void OnPart(IrcConnection connection, Irc.Actor who, string channel) {
			Begin(()=>{
				var view = ViewOf(connection,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, who + " has left the channel", system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnPrivMsg(IrcConnection connection, Irc.Actor who, string target, string message) {
			Begin(()=>{
				var view = ViewOf(connection,target);
				if ( view==null ) return;

				var style
					= connection.ActualNickname == who.Nickname     ? self
					: message.Contains( connection.ActualNickname ) ? alerted
					: normal
					;

				Match m;
				if ( (m=new Regex("\u0001ACTION (?'action'.+)\u0001").Match(message)).Success ) {
					AddPrettyHistory( view, who.Nickname, Timestamp, m.Groups["action"].Value, style );
				} else {
					AddPrettyHistory( view, "<"+who.Nickname+">", Timestamp, message, style );
				}
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnNotice(IrcConnection connection, Irc.Actor who, string target, string message) {
			Begin(()=>{
				var view = ViewOf(connection,target);
				if ( view==null ) return;

				var style
					= connection.ActualNickname == who.Nickname     ? self
					: message.Contains( connection.ActualNickname ) ? alerted
					: normal
					;

				Match m;
				if ( (m=new Regex("\u0001ACTION (?'action'.+)\u0001").Match(message)).Success ) {
					AddHistory( view, who.Nickname, Timestamp, m.Groups["action"].Value, style );
				} else {
					AddHistory( view, "<"+who.Nickname+">", Timestamp, message, style );
				}
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnQuit(IrcConnection connection, Irc.Actor who, string channel, string message) {
			Begin(()=>{
				var view = ViewOf(connection,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, who + " has quit " + connection.ConnectionID.Hostname + (message=="" ? "" : (" ("+message+")")), system );
				Invalidate();
			});
		}

		public void OnTopic( IrcConnection connection, Irc.Actor who, string channel, string topic ) {
			Begin(()=>{
				var view = ViewOf(connection,channel);
				if ( view==null ) return;

				if ( who == null ) {
					AddHistory( view, "TOPIC", Timestamp, topic, system );
				} else {
					AddHistory( view, "", Timestamp, who.Nickname + " has changed the topic to " + topic, system );
				}
				Invalidate();
			});
		}

		public void OnError( Exception e ) {
			Begin(()=>{
				// TODO:  Display ERROR
			});
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
