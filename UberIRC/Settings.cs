// Copyright Michael B. E. Rickert 2009-2010
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using UberIRC.Providers;

namespace UberIRC {
	public class Settings {
		public class Channel {
			public string Name;
			public Keys   Shortcut;

			public bool AutoJoin;
			public bool AutoRejoin;
		}

		public class Server {
			public string Name;
			public Uri    Uri;

			public bool AutoConnect;
			public bool AutoReconnect;
			public string Nickname;
			public string Username;
			public string Password;
			public string Userhost;
			public string Realname;

			public List<Channel> Channels = new List<Channel>();
		}

		Provider[] Providers = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(t=>t.IsSubclassOf(typeof(Provider)))
			.Select(t=>t.GetConstructor(Type.EmptyTypes).Invoke(null) as Provider)
			.ToArray()
			;

		private void Inject( Settings settings ) { foreach ( var provider in Providers ) provider.Settings = settings; }
		public  void Inject( IrcView  view     ) { foreach ( var provider in Providers ) provider.View     = view; }

		public IEnumerable< KeyValuePair<String,Command> > Commands { get {
			foreach ( var provider in Providers ) foreach ( var command in provider.Commands ) yield return command;
		}}

		public IEnumerable< KeyValuePair<Keys,Action> > Shortcuts { get {
			foreach ( var provider in Providers ) foreach ( var shortcut in provider.Shortcuts ) yield return shortcut;
		}}

		public Server DefaultServerSettings { get; private set; }

		Dictionary<string,Keys> AltKeyNames = new Dictionary<string,Keys>()
			{ { "0", Keys.D0 }
			, { "1", Keys.D1 }, { "2", Keys.D2 }, { "3", Keys.D3 }
			, { "4", Keys.D4 }, { "5", Keys.D5 }, { "6", Keys.D6 }
			, { "7", Keys.D7 }, { "8", Keys.D8 }, { "9", Keys.D9 }
			, { "Ctrl", Keys.Control }
			, { "Backspace", Keys.Back }
			};
		public Keys ReadKeys( string keys ) {
			int k = (int)Keys.None;

			foreach ( var key in keys.Split('+') )
			if ( AltKeyNames.ContainsKey(key) )
			{
				k |= (int)AltKeyNames[key];
			}
			else
			{
				k |= (int)Enum.Parse( typeof(Keys), key );
			}
			return (Keys)k;
		}

		public Channel ReadChannel( XmlNode channel ) {
			var ch = new Channel();

			foreach ( XmlAttribute attribute in channel.Attributes )
			switch ( attribute.Name )
			{
			case "name"    : ch.Name = attribute.Value; break;
			case "shortcut": ch.Shortcut = ReadKeys(attribute.Value); break;
			case "auto":
				foreach ( string auto in attribute.Value.Split(',') )
				switch ( auto )
				{
				case "join"  : ch.AutoJoin   = true; break;
				case "rejoin": ch.AutoRejoin = true; break;
				default: throw new FormatException( "Unexpected id for auto attribute, "+auto );
				}
				break;
			default:
				throw new FormatException( "Unexpected attribute for <channel> tag, "+attribute.Name );
			}

			if ( ch.Name == null ) throw new FormatException( "<channel> tag needs a name attribute" );

			return ch;
		}

		public Server ReadServer( XmlNode server ) {
			if ( server==null ) return null;

			var s = new Server();

			foreach ( XmlAttribute attribute in server.Attributes )
			switch ( attribute.Name )
			{
			case "name": s.Name     = attribute.Value; break;
			case "url" :
				try { s.Uri = new Uri(attribute.Value); }
				catch ( UriFormatException ) { s.Uri = new UriBuilder() { Scheme = "irc", Host = attribute.Value }.Uri; }
				break;
			case "nick": s.Nickname = attribute.Value; break;
			case "user": s.Username = attribute.Value; break;
			case "pass": s.Password = attribute.Value; break;
			case "auto":
				foreach ( string auto in attribute.Value.Split(',') )
				switch ( auto )
				{
				case "connect"  : s.AutoConnect   = true; break;
				case "reconnect": s.AutoReconnect = true; break;
				default: throw new FormatException( "Unexpected id for auto attribute, "+auto );
				}
				break;
			default:
				throw new FormatException( "Unexpected attribute for <server> tag, "+attribute.Name );
			}

			if ( server.Name != "defaults" && s.Uri == null ) throw new FormatException( "<server> tag needs a url attribute" );

			foreach ( XmlNode channel in server.SelectNodes("./channel") ) s.Channels.Add( ReadChannel(channel) );

			return s;
		}

		public string Path { get; private set; }
		public XmlDocument XML { get; private set; }
		public Settings( string path ) {
			Path = path;
			Reload();
			Inject(this);
		}

		public void Reload() {
			using ( var reader = File.Open(Path,FileMode.Open,FileAccess.Read,FileShare.Read) ) {
				XML = new XmlDocument();
				XML.Load(reader);
			}
			DefaultServerSettings
				= ReadServer( XML.SelectSingleNode("//defaults") )
				?? new Server()
					{ AutoConnect   = false
					, AutoReconnect = true
					}
				;
			if ( DefaultServerSettings.Nickname == null ) DefaultServerSettings.Nickname = "UberGuest";
			if ( DefaultServerSettings.Username == null ) DefaultServerSettings.Username = "uberirc";
			if ( DefaultServerSettings.Userhost == null ) DefaultServerSettings.Userhost = "*";
			if ( DefaultServerSettings.Realname == null ) DefaultServerSettings.Realname = "*";
		}

		public IEnumerable<Server> Servers { get {
			foreach ( XmlNode serverxml in XML.SelectNodes("//server") ) {
				var server = ReadServer(serverxml);
				server.Nickname = server.Nickname ?? DefaultServerSettings.Nickname;
				server.Username = server.Username ?? DefaultServerSettings.Username;
				server.Userhost = server.Userhost ?? DefaultServerSettings.Userhost;
				server.Realname = server.Realname ?? DefaultServerSettings.Realname;
				yield return server;
			}
		}}
	}
}
