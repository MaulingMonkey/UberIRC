// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Xml;

namespace UberIRC.Providers
{
	[ProviderConfig( Enabled=true )]
	class QuoteProvider : Provider {
		// <quote command="/quote" />

		public override IEnumerable< KeyValuePair<String,Command> > Commands { get {
			foreach ( XmlNode node in Settings.XML.SelectNodes("//quote") ) {
				string command = null;

				foreach ( XmlAttribute attribute in node.Attributes )
				switch ( attribute.Name )
				{
				case "command": command = attribute.Value; break;
				default:
					throw new FormatException( "Unexpected attribute "+attribute.Name+" in <search/> tag" );
				}

				if ( command == null ) throw new FormatException( "Expected an attribute, command, in <quote/> tag" );
				if ( command.StartsWith("/") ) command = command.Substring(1);

				var c = new Command( (p) => {
					View.BeginTrySendRaw(p);
				});

				yield return new KeyValuePair<String,Command>( command, c );
			}
		} }
	}
}
