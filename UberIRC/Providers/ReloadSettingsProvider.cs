// Copyright Michael B. E. Rickert 2010
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;

namespace UberIRC.Providers {
	[ProviderConfig( Enabled=true )]
	class ReloadSettingsProvider : Provider {
		// <reload shortcut="F5" />

		public override IEnumerable<KeyValuePair<Keys, Action>> Shortcuts { get {
			foreach ( XmlNode node in Settings.XML.SelectNodes("//reload") ) {
				string shortcut = null;

				foreach ( XmlAttribute attribute in node.Attributes )
				switch ( attribute.Name )
				{
				case "shortcut": shortcut = attribute.Value; break;
				default:
					throw new FormatException( "Unexpected attribute "+attribute.Name+" in <search/> tag" );
				}

				yield return new KeyValuePair<Keys,Action>( Settings.ReadKeys(shortcut), ()=> {
					Settings.Reload();
				});
			}
		}}
	}
}
