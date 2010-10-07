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
			public IrcChannelID    ID;
			public ChatHistory     History;
			public ChannelSelector ChannelSelector;
			public TextBox         Input;
			public bool            IsPerson;
			public bool            IsUnread, IsHighlighted, IsHidden, IsHiddenPermanently;

			public readonly int Margin = 2;
			public Channel( IrcChannelID id, Font.Library Library, Size ClientSize ) {
				ID = id;
				History = new ChatHistory()
					{ Bounds = new Rectangle( Margin, Margin, ClientSize.Width-2*Margin, ClientSize.Height-2*Margin )
					};
				ChannelSelector = new ChannelSelector()
					{
					};
				Input = new TextBox()
					{ MaxBounds = new Rectangle( 1*Margin, ClientSize.Height-100-Margin, ClientSize.Width-2*Margin, 100 )
					, Font = new Font( Library, "Uber Console", 5 ) { Color = Color.Black }
					, Text = ""
					, VerticalAlignment = VerticalAlignment.Bottom
					};
			}
		}

		Channel CreateChannel(IrcChannelID id,bool person) {
			var channel = new Channel(id,library,ClientSize) { IsPerson = person };
			AddHistory( channel, person?"PERSON":"CHANNEL", "", id.Channel, normal );
			Views.Add( id, channel );
			if ( CurrentView == null ) CurrentView = channel;
			return channel;
		}
	}
}
