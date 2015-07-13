// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Industry;
using UberIRC.NET;

namespace UberIRC {
	[System.ComponentModel.DesignerCategory("")]
	public partial class IrcView : Form, IDisposable {
		Settings Settings;
		Dictionary<IrcChannelID,Channel> Views;
		Dictionary<Keys        ,Action > Shortcuts;
		Dictionary<String      ,Command> Commands;

		IEnumerable<Channel> VisibleOrderedChannels { get { return Views.Values.Where(ch=>!ch.IsHidden).OrderBy(ch=>ch.IsPerson?1:0); }}

		public String ProviderDisplayNickname { get {
			string nick = null;
			Invoke( new Action( () => {
				if ( CurrentView == null ) nick = "UberIRC";
				else nick = CurrentView.ID.Connection.ActualNickname.ToString() ?? CurrentView.ID.Connection.TargetNickname.ToString();
			}));
			return nick;
		} }

		public void BeginTryPasteLink( string url ) {
			Begin( () => {
				if ( CurrentView == null ) return;
				bool spaceit
					= CurrentView.Input.Text.Length!=0
					&& !CurrentView.Input.Text.EndsWith(" ")
					;
				CurrentView.Input.Text += (spaceit?" ":"") + url;
			});
		}
		
		void IDisposable.Dispose() {
			base.Dispose();
			RAII.Dispose(this);
		}

		public IrcView( Settings settings ) {
			Views = new Dictionary<IrcChannelID,Channel>();

			irc = new Irc(settings);
			irc.Listeners.Add(this);

			Settings = settings;
			Settings.Inject(this);

			cursorblink = new Timer() { Interval = 500 };
			cursorblink.Tick += (o,args) => { cursor = !cursor; if ( CurrentView != null ) Invalidate( CurrentView.Input.Bounds ); };
			cursorblink.Start();

			InitializeComponent();
			InitializeShortcutsAndCommands();
			InitializeStyle();

			foreach ( var server in settings.Servers ) {
				var connection = irc.Connect(server.Uri);

				foreach ( var channel in server.Channels ) {
					connection.Join( channel.Name );
					if ( channel.Shortcut == Keys.None ) continue;
					string cname = channel.Name;
					Shortcuts.Add( channel.Shortcut, () => CurrentView = ViewOf(connection,null,cname) );
				}
			}
		}

		void InitializeComponent() {
			SuspendLayout();
			
			AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			BackColor = System.Drawing.Color.White;
			ClientSize = new System.Drawing.Size(292, 273);
			DoubleBuffered = true;
			Name = "IrcView";
			Text = "UberIRC";

			Paint     += IrcView_Paint;
			KeyPress  += IrcView_KeyPress;
			Resize    += IrcView_Resize;
			KeyDown   += IrcView_KeyDown;
			MouseDown += IrcView_MouseDown;
			
			ResumeLayout(false);
		}

		void InitializeShortcutsAndCommands() {
			Shortcuts = new Dictionary<Keys,Action>()
				{ { Keys.None, null }
				, { Keys.PageUp                   , () => CurrentView.History.PageUp(10) }
				, { Keys.PageDown                 , () => CurrentView.History.PageDown(10) }
				, { Keys.PageUp  |Keys.Shift      , () => CurrentView.History.PageUp(1) }
				, { Keys.PageDown|Keys.Shift      , () => CurrentView.History.PageDown(1) }
				, { Keys.PageUp  |Keys.Control    , () => CurrentView.History.PageUp(100) }
				, { Keys.PageDown|Keys.Control    , () => CurrentView.History.PageDown(100) }
				, { Keys.X|Keys.Control           , Cut }
				, { Keys.C|Keys.Control           , Copy }
				, { Keys.V|Keys.Control           , Paste }
				, { Keys.W|Keys.Control           , ()=>HideView(false) }
				, { Keys.W|Keys.Control|Keys.Shift, ()=>HideView(true)  }
				, { Keys.BrowserForward           , NextView }
				, { Keys.BrowserBack              , PrevView }
				, { Keys.Left |Keys.Control       , PrevView }
				, { Keys.Right|Keys.Control       , NextView }
				, { Keys.Enter                    , OnEnter }
				, { Keys.Tab                      , AttemptTabComplete }
				};

			Commands = new Dictionary<string,Command>()
				{ { "join"      , Join }
				, { "part"      , Part }
				, { "leave"     , Part }
				, { "say"       , SendMessage }
				, { "me"        , SendAction }
				, { "invite"    , SendInvite }
				, { "pm"        , SendPrivateMessage }
				, { "msg"       , SendPrivateMessage }
				, { "nick"      , ChangeNick }
				, { "topic"     , Topic }
				, { "o/"        , rest => SendMessage("/o/ "+rest) }
				, { "kick"      , Kick }
				, { "ban"       , Ban }
				, { "kickban"   , KickBan }
				, { "kb"        , KickBan }
				, { "unban"     , UnBan }
				, { "mode"      , ChangeModes }
				, { "ignore"    , Ignore }
				, { "unignore"  , UnIgnore }
				, { "semiignore", SemiIgnore }
				, { "twit"      , Baddy }
				, { "evil"      , Baddy }
				, { "baddy"     , Baddy }
				, { "baddie"    , Baddy }
				, { "untwit"    , UnBaddy }
				, { "unevil"    , UnBaddy }
				, { "unbaddy"   , UnBaddy }
				, { "unbaddie"  , UnBaddy }
				};
		}

		void Cut() {
			if ( CurrentView == null ) return;
			Clipboard.SetText( CurrentView.Input.Text ?? "" );
			CurrentView.Input.Text = "";
		}

		void Copy() {
			if ( CurrentView == null ) return;
			Clipboard.SetText( CurrentView.Input.Text ?? "" );
		}

		void Paste() {
			if ( CurrentView == null ) return;
			CurrentView.Input.Text += Clipboard.GetText() ?? "";
		}

		void HideView( bool permanently ) {
			if (CurrentView==null) return;
			var original = CurrentView;

			NextView();
			if ( CurrentView==original ) PrevView();
			if ( CurrentView==original ) return;

			original.IsHidden = true;
			if ( permanently && !CurrentView.IsPerson ) original.IsHiddenPermanently = true;
		}

		void NextView() {
			Channel prev = null;
			foreach ( var chan in VisibleOrderedChannels ) {
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
			foreach ( var chan in VisibleOrderedChannels ) {
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
			var nicks = CurrentView.ID.Connection.WhosIn(CurrentView.ID.Channel).Select(cui=>cui.Nick);

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
				AddHistory( CurrentView, "Tab Complete", Timestamp, String.Join(", ",possible), system );
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

		void Ban( string p ) {
			if ( CurrentView == null ) return;
			CurrentView.ID.Connection.Ban( CurrentView.ID.Channel, p );
		}

		void UnBan( string p ) {
			if ( CurrentView == null ) return;
			CurrentView.ID.Connection.UnBan( CurrentView.ID.Channel, p );
		}

		void KickBan( string p ) {
			if ( CurrentView == null ) return;
			if ( p.Contains(' ') ) {
				int s = p.IndexOf(' ');
				CurrentView.ID.Connection.KickBan( CurrentView.ID.Channel, p.Substring(0,s), p.Substring(s+1) );
			} else {
				CurrentView.ID.Connection.KickBan( CurrentView.ID.Channel, p, "" );
			}
		}

		void ChangeModes( string modetext ) {
			if ( CurrentView == null ) return;
			var modes = new Irc.ModeChangeSet(modetext);
			CurrentView.ID.Connection.ChangeModes( CurrentView.ID.Channel, modes );
		}

		private void IrcView_Resize(object sender, EventArgs e) {
			foreach ( var view in Views.Values ) view.Input.MaxBounds.Y = ClientSize.Height-100-CurrentView.Margin;
			Invalidate();
		}

		bool cursor = true;
		[Owns] Timer cursorblink;

		private void IrcView_Paint(object sender, PaintEventArgs e) {
			if ( CurrentView == null ) return;

			CurrentView.IsUnread = CurrentView.IsHighlighted = CurrentView.IsHidden = false;
			CurrentView.ChannelSelector.Selected = normal .Message .Font;
			CurrentView.ChannelSelector.Inactive = system .Nickname.Font;
			CurrentView.ChannelSelector.Active   = normal .Nickname.Font;
			CurrentView.ChannelSelector.Alerted  = alerted.Nickname.Font;
			CurrentView.ChannelSelector.SelectedChannel = CurrentView;
			CurrentView.ChannelSelector.Channels        = VisibleOrderedChannels;

			CurrentView.UserList.Normal = normal.Message.Font;

			CurrentView.ChannelSelector.Bounds.Height = CurrentView.ChannelSelector.RequestedSize.Height;
			CurrentView.ChannelSelector.Bounds.X = CurrentView.Margin;
			CurrentView.ChannelSelector.Bounds.Y = CurrentView.Input.Bounds.Top - 2*CurrentView.Margin - CurrentView.ChannelSelector.Bounds.Height;

			CurrentView.History.Width  = CurrentView.Input.MaxBounds.Width = CurrentView.ChannelSelector.Bounds.Width = ClientSize.Width - 2*CurrentView.Margin;
			CurrentView.History.Height = CurrentView.ChannelSelector.Bounds.Top - CurrentView.History.Bounds.Top - 2*CurrentView.Margin - 1;

			bool show_userlist = CurrentView.UserList.RequestedSize != Size.Empty;

			if ( show_userlist ) {
				CurrentView.History.Width -= 2*CurrentView.Margin+CurrentView.UserList.RequestedSize.Width;
				CurrentView.UserList.Bounds = new Rectangle()
					{ X = CurrentView.History.X + CurrentView.History.Width + 2
					, Y = CurrentView.History.Y
					, Width = CurrentView.UserList.RequestedSize.Width
					, Height = CurrentView.History.Height
					};
			}

			if ( e.ClipRectangle.IntersectsWith(CurrentView.History.Bounds) ) CurrentView.History.RenderTo( e.Graphics );
			CurrentView.ChannelSelector.RenderTo( e.Graphics );
			if ( show_userlist ) CurrentView.UserList.RenderTo( e.Graphics );
			using ( var seperator_pen = new Pen(Color.FromArgb(unchecked((int)0xFFBBBBBBu))) ) {
				e.Graphics.DrawLine
					( seperator_pen
					, new Point
						( CurrentView.Margin
						, CurrentView.History.Bounds.Bottom + CurrentView.Margin
						)
					, new Point
						( ClientSize.Width - 2*CurrentView.Margin
						, CurrentView.History.Bounds.Bottom + CurrentView.Margin
						)
					);
				e.Graphics.DrawLine
					( seperator_pen
					, new Point
						( CurrentView.Margin
						, CurrentView.ChannelSelector.Bounds.Bottom + CurrentView.Margin
						)
					, new Point
						( ClientSize.Width - 2*CurrentView.Margin
						, CurrentView.ChannelSelector.Bounds.Bottom + CurrentView.Margin
						)
					);
				if ( show_userlist ) e.Graphics.DrawLine
					( seperator_pen
					, new Point
						( CurrentView.UserList.Bounds.Left-CurrentView.Margin
						, CurrentView.UserList.Bounds.Top
						)
					, new Point
						( CurrentView.UserList.Bounds.Left-CurrentView.Margin
						, CurrentView.UserList.Bounds.Bottom+CurrentView.Margin
						)
					);
			}
			CurrentView.Input.RenderTo( e.Graphics, cursor );
		}

		private void IrcView_KeyDown(object sender, KeyEventArgs e) {
			if ( Shortcuts.ContainsKey(e.KeyData) ) {
				e.SuppressKeyPress = true;
				Shortcuts[e.KeyData]();
			} else {
				var sc = Settings.Shortcuts.FirstOrDefault( s => s.Key == e.KeyData ).Value;
				if ( sc != null ) {
					e.SuppressKeyPress = true;
					sc();
				}
			}
			Invalidate();
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
					Sounds.Beep.Play(this);
				}
				break;
			default:
				CurrentView.Input.Text += e.KeyChar;
				Invalidate();
				break;
			}
		}

		void IrcView_MouseDown(object sender, MouseEventArgs e) {
			if ( CurrentView == null ) return;
			if ( e.Button == MouseButtons.Left ) CurrentView.History.ClickAt(e.X,e.Y);
		}
	}
}
