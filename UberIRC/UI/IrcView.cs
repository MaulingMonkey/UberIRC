// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
using Industry;
using UberIRC.NET;

namespace UberIRC {
	public partial class IrcView : Form, IDisposable {
		Settings Settings;
		Dictionary<IrcChannelID,Channel> Views;
		Dictionary<Keys        ,Action > Shortcuts;
		Dictionary<String      ,Command> Commands;
		
		void IDisposable.Dispose() {
			base.Dispose();
			RAII.Dispose(this);
		}

		public IrcView( Settings settings ) {
			Settings = settings;
			Settings.Inject(this);

			Views = new Dictionary<IrcChannelID,Channel>();

			irc = new Irc(settings);

			cursorblink = new Timer() { Interval = 500 };
			cursorblink.Tick += (o,args) => { cursor = !cursor; Invalidate(); };
			cursorblink.Start();

			InitializeComponent();
			InitializeShortcutsAndCommands();
			InitializeStyle();
			InitializeIO();

			foreach ( var server in settings.Servers ) {
				var connection = irc.Connect(server.Uri);

				foreach ( var channel in server.Channels ) {
					connection.Join( channel.Name );
					if ( channel.Shortcut == Keys.None ) continue;
					Shortcuts.Add( channel.Shortcut, () => CurrentView = ViewOf(connection,channel.Name) );
				}
			}
		}

		void InitializeShortcutsAndCommands() {
			Shortcuts = new Dictionary<Keys,Action>()
				{ { Keys.None, null }
				, { Keys.PageUp            , () => CurrentView.History.PageUp() }
				, { Keys.PageDown          , () => CurrentView.History.PageDown() }
				, { Keys.X|Keys.Control    , Cut }
				, { Keys.C|Keys.Control    , Copy }
				, { Keys.V|Keys.Control    , Paste }
				, { Keys.V|Keys.Control|Keys.Shift, Pastebin }
				, { Keys.BrowserForward    , NextView }
				, { Keys.BrowserBack       , PrevView }
				, { Keys.Left |Keys.Control, PrevView }
				, { Keys.Right|Keys.Control, NextView }
				, { Keys.Enter             , OnEnter }
				, { Keys.Back              , () => CurrentView.Input.Backspace() }
				, { Keys.Tab               , AttemptTabComplete }
				};

			Commands = new Dictionary<string,Command>()
				{ { "join", Join }
				, { "part", Part }
				, { "say" , SendMessage }
				, { "me"  , SendAction }
				, { "nick", ChangeNick }
				, { "o/"  , rest => SendMessage("/o/ "+rest) }
				, { "kick", Kick }
				, { "mode", ChangeModes }
				};

			foreach ( var command in Settings.Commands ) Commands.Add( command.Key, command.Value );
		}

		void Cut() {
			if ( CurrentView == null ) return;
			Clipboard.SetText( CurrentView.Input.Text );
			CurrentView.Input.Text = "";
		}

		void Copy() {
			if ( CurrentView == null ) return;
			Clipboard.SetText( CurrentView.Input.Text );
		}

		void Paste() {
			if ( CurrentView == null ) return;
			CurrentView.Input.Text += Clipboard.GetText();
		}

		Dictionary< string, HashSet<string> > PastebinLanguageKeywords = new Dictionary<string,HashSet<string>>()
				{ { "cpp", new HashSet<string>() { "#include", "#define", "namespace std", "namespace boost", "::", "->" } }
				, { "csharp", new HashSet<string>() { "unsafe", "System.", "this.", "throw new", "yield return" } }
				};
		void Pastebin() {
			string language = "csharp"; // default
			var code = Clipboard.GetText();

			foreach ( var entry in PastebinLanguageKeywords ) {
				foreach ( var keyword in entry.Value )
				if ( code.Contains(keyword) )
				{
					language = entry.Key;
					break;
				}
			}

			using ( var client = new WebClient() ) {
				var p
					= "paste=Send"
					+ "&format="+language
					+ "&poster="+HttpUtility.UrlEncode(CurrentView.ID.Connection.ActualNickname)
					+ "&expiry=m" // d = day, m = month, f = forever
					+ "&code2="+HttpUtility.UrlEncode(code)
					;

				client.Encoding = Encoding.UTF8;
				client.Headers.Add( "Content-Type", "application/x-www-form-urlencoded" );
				client.UploadStringCompleted += new UploadStringCompletedEventHandler(PastebinUploadStringCompleted);
				client.UploadStringAsync( new Uri("http://gamedev.pastebin.com/pastebin.php"), p );
			}
		}

		// <li class="highlight"><a href="http://gamedev.pastebin.com/m359e6558">DebugMonkey</a><br/>1 sec ago</li>
		Regex pastebinpostmatcher = new Regex("<li class=\"highlight\"><a href=\"(.+)\">.+</a><br/>\\d+ sec ago</li>");

		void PastebinUploadStringCompleted(object sender, UploadStringCompletedEventArgs e) {
			string result = e.Result;
			Match m = pastebinpostmatcher.Match( result ); // use client.ResponseHeaders instead?
			if ( m.Success ) BeginInvoke( new Action( () => {
				if ( CurrentView != null ) CurrentView.Input.Text += (CurrentView.Input.Text.Length==0 || CurrentView.Input.Text.EndsWith(" ") ? "" : " ") + m.Groups[1].Value;
			}));
		}

		void NextView() {
			Channel prev = null;
			foreach ( var chan in Views.Values ) {
				if ( prev == CurrentView ) {
					CurrentView = chan;
					return;
				}
				prev = chan;
			}
			CurrentView = prev; // last view
		}

		void PrevView() {
			Channel first = null;
			Channel prev = null;
			foreach ( var chan in Views.Values ) {
				if ( first == null ) first = chan;
				if ( chan == CurrentView ) {
					if ( prev != null ) CurrentView = prev;
					return;
				}
				prev = chan;
			}
			CurrentView = first; // first view
		}

		void AttemptTabComplete() {
			if ( CurrentView == null ) return;
			var nicks = CurrentView.ID.Connection.WhosIn(CurrentView.ID.Channel);

			var input = CurrentView.Input.Text;
			var lastspace = input.LastIndexOf(' ');
			if ( lastspace == input.Length-1 ) return; // cannot tab-complete spaces
			var tocomplete = (lastspace==-1) ? input : input.Substring(lastspace+1);
			var root       = (lastspace==-1) ? "" : input.Substring(0,lastspace+1);

			var possible = (from nick in nicks where nick.StartsWith(tocomplete) select nick).ToArray();
			
			if ( possible.Length == 1 ) {
				CurrentView.Input.Text = root + possible[0];
			} else if ( possible.Length > 1 ) {
				string common = possible[0];
				int longest = common.Length;
				
				foreach ( var possibility in possible ) {
					if ( possibility.Length < longest ) longest = possibility.Length;
					
					for ( int i = 0 ; i < longest ; ++i )
					if ( possibility[i] != common[i] )
					{
						longest = i;
						break;
					}
				}

				CurrentView.Input.Text = root + common.Substring(0,longest);
			}
		}

		void Kick( string p ) {
			if ( CurrentView == null ) return;
			if ( p.Contains(' ') ) {
				int s = p.IndexOf(' ');
				CurrentView.ID.Connection.Kick( CurrentView.ID.Channel, p.Substring(0,s), p.Substring(s+1) );
			} else {
				CurrentView.ID.Connection.Kick( CurrentView.ID.Channel, p, "" );
			}
		}

		void ChangeModes( string modetext ) {
			if ( CurrentView == null ) return;
			var modes = new Irc.ModeChangeSet(modetext);
			CurrentView.ID.Connection.ChangeModes( CurrentView.ID.Channel, modes );
		}

		private void IRCView_Resize(object sender, EventArgs e) {
			foreach ( var view in Views.Values ) view.Input.MaxBounds.Y = ClientSize.Height-100-CurrentView.Margin;
			Invalidate();
		}

		bool cursor = true;
		[Owns] Timer cursorblink;

		private void IRCView_Paint(object sender, PaintEventArgs e) {
			if ( CurrentView == null ) return;

			CurrentView.History.Width  = CurrentView.Input.MaxBounds.Width = ClientSize.Width - 2*CurrentView.Margin;
			CurrentView.History.Height = CurrentView.Input.Bounds.Top - CurrentView.History.Bounds.Top - 2*CurrentView.Margin - 1;

			CurrentView.History.RenderTo( e.Graphics );
			e.Graphics.DrawLine
				( new Pen(Color.FromArgb(unchecked((int)0xFFBBBBBBu)))
				, new Point
					( CurrentView.Margin
					, CurrentView.History.Bounds.Bottom + CurrentView.Margin
					)
				, new Point
					( ClientSize.Width - 2*CurrentView.Margin
					, CurrentView.History.Bounds.Bottom + CurrentView.Margin
					)
				);
			CurrentView.Input.RenderTo( e.Graphics, cursor );
		}

		private void IrcView_KeyDown(object sender, KeyEventArgs e) {
			if ( Shortcuts.ContainsKey(e.KeyData) ) {
				e.SuppressKeyPress = true;
				Shortcuts[e.KeyData]();
				Invalidate();
			}
		}

		private void IrcView_KeyPress(object sender, KeyPressEventArgs e) {
			if ( CurrentView == null ) return;

			switch ( e.KeyChar ) {
			case '\b':
				var text = CurrentView.Input.Text;
				if ( text.Length > 0 ) {
					CurrentView.Input.Text = text.Substring(0,text.Length-1);
					Invalidate();
				} else {
					MessageBeep(MB_ICONEXCLAMATION);
				}
				break;
			default:
				CurrentView.Input.Text += e.KeyChar;
				Invalidate();
				break;
			}
		}
	}
}
