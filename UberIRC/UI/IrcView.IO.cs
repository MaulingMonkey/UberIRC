// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Industry;
using Industry.FX;
using UberIRC.NET;

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
			Invalidate();
		}

		const string fUrlProtocol = @"([^\s]+?:\/\/[^\s]+?)";
		const string fUrlTLD      = @"([^\s]+?\.(?:com|net|org|edu|gov|mil|info|biz)[^\s]*?)";
		const string fUrlBLD      = @"((?:www|ftp)\.[^\s]+?)";

		static readonly Regex reUrlProtocol = new Regex("^"+fUrlProtocol);
		static readonly Regex reUrlPatterns = new Regex
			(@"\b(?:" + fUrlProtocol
			+ "|"  + fUrlTLD
			+ "|"  + fUrlBLD
			+@")(?=\s|$)"
			);

		readonly static string[] SafeProtocols = new[] { "http", "ftp", "https" };
		readonly static string[] UnsafeProtocols = new[] { "file" };

		bool IsUrlSafeToOpenWithFPH( string url ) {
			foreach ( var proto in SafeProtocols ) if ( url.StartsWith( proto+"://" ) ) return true;
			foreach ( var proto in UnsafeProtocols ) if ( url.StartsWith( proto+"://" ) ) {
				MessageBox.Show
					( this
					, "UberIRC refuses to open this link (unsafe protocol)\n\n"+url
					, "Protocol "+proto+" forbidden"
					, MessageBoxButtons.OK
					, MessageBoxIcon.Error
					, MessageBoxDefaultButton.Button1
					);
				return false;
			}

			var result = MessageBox.Show
				( this
				, "Are you sure you want to open this link?\nUberIRC doesn't know if it's safe...\n\n"+url
				, "Le Safety Dialogue~"
				, MessageBoxButtons.YesNo
				, MessageBoxIcon.Warning
				, MessageBoxDefaultButton.Button2
				);
			switch ( result ) {
			case DialogResult.OK:
			case DialogResult.Yes:
				return true;
			case DialogResult.Abort:
			case DialogResult.Cancel:
			case DialogResult.No:
			case DialogResult.None:
				return false;
			default:
				Debug.Fail( "Invalid MessageBox.Show return value", "Returned "+Enum.GetName(typeof(DialogResult),result)+", expected OK, Yes, Abort, Cancel, No, or None." );
				return false;
			}
		}

		string GuessAndPrependProtocol( string url ) {
			Match m = reUrlProtocol.Match(url);
			
			if ( m.Success ) return url;
			else if ( url.StartsWith("www.") ) return "http://"+url;
			else if ( url.StartsWith("ftp.") ) return "ftp://"+url;
			else return "http://"+url;
		}

		IEnumerable<TextRun> ToPrettyRuns( string message, TextStyle style ) {
			int lasturlend = 0;

			foreach ( Match match in reUrlPatterns.Matches(message) ) {
				string url = match.Value;
				if ( lasturlend != match.Index ) yield return new TextRun() { Font = style.Message.Font, Text = message.Substring(lasturlend,match.Index-lasturlend) };
				string realurl = GuessAndPrependProtocol(url);

				var command = new Action(() => {
					if ( !IsUrlSafeToOpenWithFPH(realurl) ) return;

					Process process = new Process();
					process.StartInfo.FileName = "rundll32.exe";
					process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + realurl;
					process.StartInfo.UseShellExecute = true;
					process.Start();
				});
				yield return new TextRun() { Font = style.Message.LinkFont, Text = url, Tag = command };
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
			if ( line.StartsWith("/ ") )
			{
				// shorthand command for "/say /"
				SendMessage(line.Remove(1,1));
			}
			else if ( line.StartsWith("//") )
			{
				// shorthand command for "/say //"
				SendMessage(line);
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
					var cmd = Settings.Commands.FirstOrDefault( c => c.Key==command ).Value;
					if ( cmd != null ) {
						cmd(parameters);
					} else {
						AddHistory( CurrentView, "ERROR", Timestamp, "Unrecognized command "+command, commanderror );
					}
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
				var chid = new IrcChannelID() { Connection=CurrentView.ID.Connection, Channel=chan };
				if (Views.ContainsKey(chid)) Views[chid].IsHidden = false;
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

		void SendPrivateMessage( string line ) {
			var split = line.Split(new[]{' '},2);
			OnPrivMsg( CurrentView.ID.Connection, new Irc.Actor() { Nickname = CurrentView.ID.Connection.ActualNickname, Hostname = "???", Username = "???" }, split[0], split[1] );
			CurrentView.ID.Connection.Send( "PRIVMSG "+split[0]+" :"+split[1] );
		}

		void SendAction( string line ) {
			line = "\u0001ACTION "+line+"\u0001";
			SendMessage(line);
		}

		void SendInvite( string line ) {
			string nick;
			string channel = CurrentView.ID.Channel;
			if ( line.Contains(' ') ) {
				var args = line.Split(' ');
				nick = args[1];
				channel = args[0];
				if ( !channel.StartsWith("#") ) channel = "#"+channel;
			} else {
				nick = line;
			}

			CurrentView.ID.Connection.Send( "INVITE "+nick+" "+channel );
		}

		public void BeginTrySendMessage( string line ) {
			BeginInvoke( new Action( () => { if ( CurrentView!=null ) SendMessage(line); } ) );
		}
		public void BeginTrySendAction( string line ) {
			BeginInvoke( new Action( () => { if ( CurrentView!=null ) SendAction(line); } ) );
		}

		public void OnChannelModeChange(IrcConnection connection, Irc.Actor op, string channel, string mode, string param) {
			Begin(()=>{
				var view = ViewOf(connection,op,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, op.Nickname + " has set mode " + mode + " " + param, system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnJoin(IrcConnection connection, Irc.Actor who, string channel) {
			Begin(()=>{
				var view = ViewOf(connection,who,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, who + " has joined the channel", system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnKick(IrcConnection connection, Irc.Actor op, string channel, string target, string message) {
			Begin(()=>{
				var view = ViewOf(connection,op,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, op.Nickname + " has kicked " + target + " from the channel" + (message=="" ? "" : (" ("+message+")")), system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnModeChange(IrcConnection connection, Irc.Actor op, string channel, string mode, string target) {
			Begin(()=>{
				var view = ViewOf(connection,op,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, op.Nickname + " has set mode " + mode + " on " + target, system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnNick(IrcConnection connection, Irc.Actor who, string channel, string new_) {
			Begin(()=>{
				var view = ViewOf(connection,who,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, who.Nickname + " is now known as " + new_, system );
				Invalidate();
			});
		}

		public void OnRplInvited(IrcConnection connection, Irc.Actor who, string channel ) {
			Begin(()=>{
				var view = ViewOf(connection,who,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, "You have invited " + who.Nickname + " to " + channel, system );
				Invalidate();
			});
		}

		public void OnPart(IrcConnection connection, Irc.Actor who, string channel) {
			Begin(()=>{
				var view = ViewOf(connection,who,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, who + " has left the channel", system );
				if ( view == CurrentView ) Invalidate();
			});
		}

		public void OnPrivMsg(IrcConnection connection, Irc.Actor who, string target, string message) {
			Begin(()=>{
				var view = ViewOf(connection,who,target);
				if ( view==null ) return;

				var style = GetStyleFor( view, connection, who, target, message );

				if ( style == null ) return;

				if ( style == alerted || target == connection.ActualNickname )
				if ( !view.IsHiddenPermanently )
				{
					MessageBeep(MB_OK);
					view.IsHighlighted = true;
				}
				view.IsUnread = true;
				view.IsHidden = view.IsHiddenPermanently;

				Match m;
				if ( (m=new Regex("\u0001ACTION (?'action'.+)\u0001").Match(message)).Success ) {
					AddPrettyHistory( view, who.Nickname, Timestamp, m.Groups["action"].Value, style );
				} else {
					AddPrettyHistory( view, "<"+who.Nickname+">", Timestamp, message, style );
				}

				Invalidate();
			});
		}

		public void OnNotice(IrcConnection connection, Irc.Actor who, string target, string message) {
			Begin(()=>{
				var view = ViewOf(connection,who,target);
				if ( view==null ) return;

				var style = GetStyleFor( view, connection, who, target, message );

				if ( style == null ) return;

				if ( style == alerted || target == connection.ActualNickname )
				if ( !view.IsHiddenPermanently )
				{
					MessageBeep(MB_OK);
					view.IsHighlighted = true;
				}
				view.IsUnread = true;
				view.IsHidden = view.IsHiddenPermanently;

				Match m;
				if ( (m=new Regex("\u0001ACTION (?'action'.+)\u0001").Match(message)).Success ) {
					AddHistory( view, who.Nickname, Timestamp, m.Groups["action"].Value, style );
				} else {
					AddHistory( view, "<"+who.Nickname+">", Timestamp, message, style );
				}

				Invalidate();
			});
		}

		public void OnQuit(IrcConnection connection, Irc.Actor who, string channel, string message) {
			Begin(()=>{
				var view = ViewOf(connection,who,channel);
				if ( view==null ) return;

				AddHistory( view, "", Timestamp, who + " has quit " + connection.ConnectionID.Hostname + (message=="" ? "" : (" ("+message+")")), system );
				Invalidate();
			});
		}

		public void OnTopic( IrcConnection connection, Irc.Actor who, string channel, string topic ) {
			Begin(()=>{
				var view = ViewOf(connection,who,channel);
				if ( view==null ) return;

				if ( who == null ) {
					AddHistory( view, "TOPIC", Timestamp, topic, system );
				} else {
					AddHistory( view, "", Timestamp, who.Nickname + " has changed the topic to " + topic, system );
				}
				Invalidate();
			});
		}

		public void OnErrNickInUse( IrcConnection connection, string nick ) {
			Begin(()=>{
				if ( CurrentView==null ) return;

				AddHistory( CurrentView, "Error:", Timestamp, "Nickname " +nick+ " is already in use." , commanderror );
				Invalidate();
			});
		}

		public void OnErrNotChannelOp( IrcConnection connection, string channel, string message ) {
			Begin(()=>{
				var view = ViewOf(connection,null,channel);
				if ( view==null ) return;
				AddHistory( view, "", Timestamp, string.IsNullOrEmpty(message) ? "You're not a channel operator" : message, commanderror );
				if ( view==CurrentView ) Invalidate();
			});
		}

		public void OnRecvParseError( IrcConnection connection, string rawline, Exception e ) {
			Begin(()=>{
				var view = ViewOf(connection,null,"Error Log");
				AddHistory( view, "Exception"    , Timestamp, e.GetType().Name + " thrown during parsing of recieved data", alerted );
				AddHistory( view, "Recieved Data", Timestamp, rawline, normal );
				AddHistory( view, "Message"      , Timestamp, e.Message, normal );
				Invalidate();
			});
		}

		public void OnConnectionError( IrcConnection connection, Exception e ) {
			Begin(()=>{
				var view = ViewOf(connection,null,"Error Log");
				AddHistory( view, "Exception"    , Timestamp, e.GetType().Name + " thrown handling of connection", alerted );
				AddHistory( view, "Message"      , Timestamp, e.Message   , normal );
				AddHistory( view, "Backtrace"    , Timestamp, e.StackTrace, normal );
			});
		}

		Channel ViewOf( IrcConnection connection, Irc.Actor who, string channel ) {
			bool pm = channel == connection.ActualNickname;
			var id = new IrcChannelID() { Connection = connection, Channel = pm?who.Nickname:channel };
			if (!Views.ContainsKey(id)) CreateChannel(id,pm);
			return Views[id];
		}

		//IEnumerable<Channel> AllViews { get { lock ( Views ) foreach ( var view in Views.Values ) yield return view; } }

		string Timestamp { get { return DateTime.Now.ToString("[HH:mm]"); } }
	}
}
