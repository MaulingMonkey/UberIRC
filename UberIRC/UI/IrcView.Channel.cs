// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System.Drawing;
using Industry.FX;
using UberIRC.NET;
using Font = Industry.FX.Font;

namespace UberIRC {
	public partial class IrcView {
		public class Channel {
			public IrcChannelID   ID;
			public ChatHistory History;
			public TextBox     Input;

			public readonly int Margin = 2;
			public Channel( IrcChannelID id, Font.Library Library, Size ClientSize ) {
				ID = id;
				History = new ChatHistory()
					{ Bounds = new Rectangle( Margin, Margin, ClientSize.Width-2*Margin, ClientSize.Height-2*Margin )
					};
				Input = new TextBox()
					{ MaxBounds = new Rectangle( 1*Margin, ClientSize.Height-100-Margin, ClientSize.Width-2*Margin, 100 )
					, Font = new Font( Library, "Uber Console", 5 ) { Color = Color.Black }
					, Text = ""
					, VerticalAlignment = VerticalAlignment.Bottom
					};
			}
		}

		Channel CreateChannel(IrcChannelID id) {
			var channel = new Channel(id,library,ClientSize);
			AddHistory( channel, "CHANNEL", "", id.Channel, normal );
			Views.Add( id, channel );
			if ( CurrentView == null ) CurrentView = channel;
			return channel;
		}
	}
}
