// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using System.Windows.Forms;
using System.Xml;

namespace UberIRC.Providers {
	class SearchProvider : Provider {
		// <search command="/google" say="google {0}" emote="googles {0}" url="http://www.google.com/search?q={0}" />

		public override IEnumerable< KeyValuePair<String,Command> > Commands { get {
			foreach ( XmlNode node in Settings.XML.SelectNodes("//search") ) {
				string command = null;
				string emote = null;
				string say = null;
				string url = null;

				foreach ( XmlAttribute attribute in node.Attributes )
				switch ( attribute.Name )
				{
				case "command": command = attribute.Value; break;
				case "emote"  : emote   = attribute.Value; break;
				case "say"    : say     = attribute.Value; break;
				case "url"    : url     = attribute.Value; break;
				
				default:
					throw new FormatException( "Unexpected attribute "+attribute.Name+" in <search/> tag" );
				}

				if ( command == null ) throw new FormatException( "Expected an attribute, command, in <search/> tag" );
				if ( emote != null && say != null ) throw new FormatException( "Expected either emote or say attributes, got both in a single <search/> tag" );
				if ( url == null ) throw new FormatException( "Expected an attribute, url, in <search/> tag" );

				if ( command.StartsWith("/") ) command = command.Substring(1);

				var c = new Command( (p) => {
					var realurl = String.Format( url, HttpUtility.UrlEncode(p) );
					if ( emote != null ) View.BeginTrySendAction ( String.Format( emote, realurl ) );
					if ( say   != null ) View.BeginTrySendMessage( String.Format( say  , realurl ) );

					Process process = new Process();
					process.StartInfo.FileName = "rundll32.exe";
					process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + realurl;
					process.StartInfo.UseShellExecute = true;
					process.Start();
				});

				yield return new KeyValuePair<String,Command>( command, c );
			}
		} }
	}
}
