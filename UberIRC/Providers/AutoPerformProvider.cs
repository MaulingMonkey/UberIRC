// Copyright Michael B. E. Rickert 2011
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Xml;

namespace UberIRC.Providers {
	[ProviderConfig( Enabled=true )]
	class AutoPerformProvider : Provider {
		// <autoperform new_channel="/action" />

		public override void OnChannelCreated( IrcView view, IrcView.Channel channel ) {
			foreach ( XmlNode node in Settings.XML.SelectNodes("//autoperform") ) {
				foreach ( XmlAttribute attribute in node.Attributes )
				switch ( attribute.Name )
				{
				case "new_channel":
					view.ExecuteOn( channel, attribute.Value );
					break;
				default:
					throw new FormatException( "Unexpected attribute "+attribute.Name+" in <autoperform/> tag" );
				}
			}
		}
	}
}
