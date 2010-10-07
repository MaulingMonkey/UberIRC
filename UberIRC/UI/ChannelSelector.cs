// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace UberIRC {
	public class ChannelSelector {
		public IEnumerable<IrcView.Channel> Channels;
		public IrcView.Channel SelectedChannel;
		public Rectangle Bounds;

		public Size RequestedSize { get {
			var measurements = Channels.Select(c=>GetFontFor(c).MeasureLine(c.ID.Channel));
			var h = measurements.Max(m=>m.Bounds.Height);
			var w = measurements.Sum(m=>m.Advance.X+10)-10;
			return new Size(w,h);
		}}

		public Industry.FX.Font Selected, Inactive, Active, Alerted;

		Industry.FX.Font GetFontFor( IrcView.Channel channel ) {
			return (channel==SelectedChannel) ? Selected
				: channel.IsHighlighted       ? Alerted
				: channel.IsUnread            ? Active
				: Inactive
				;
		}

		public void RenderTo( Graphics fx ) {
			int x = Bounds.Left;

			foreach ( var channel in Channels ) {
				var font = GetFontFor(channel);
				var name = channel.ID.Channel;
				var m = font.MeasureLine(name);
				font.RenderLineTo( fx, name, new Rectangle(x,Bounds.Top,Bounds.Right-x,Bounds.Height), Industry.FX.HorizontalAlignment.Left, Industry.FX.VerticalAlignment.Center );
				x += m.Advance.X + 10;
			}
		}
	}
}
