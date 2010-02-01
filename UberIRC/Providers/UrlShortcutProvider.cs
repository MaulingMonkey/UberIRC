// Copyright Michael B. E. Rickert 2010
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;

namespace UberIRC.Providers {
	class UrlShortcutProvider : Provider {
		// <url shortcut="Ctrl+L" url="http://logs.pandamojo.com/" />

		public override IEnumerable<KeyValuePair<Keys, Action>> Shortcuts { get {
			foreach ( XmlNode node in Settings.XML.SelectNodes("//url") ) {
				string shortcut = null;
				string url = null;

				foreach ( XmlAttribute attribute in node.Attributes )
				switch ( attribute.Name )
				{
				case "shortcut": shortcut = attribute.Value; break;
				case "url"     : url      = attribute.Value; break;
				default:
					throw new FormatException( "Unexpected attribute "+attribute.Name+" in <search/> tag" );
				}

				var c = new Action( () => {
					Process process = new Process();
					process.StartInfo.FileName = "rundll32.exe";
					process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + url;
					process.StartInfo.UseShellExecute = true;
					process.Start();
				});

				yield return new KeyValuePair<Keys,Action>( Settings.ReadKeys(shortcut), c );
			}
		}}
	}
}
