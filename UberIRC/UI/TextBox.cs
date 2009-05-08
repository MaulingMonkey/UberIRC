// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Drawing;
using Industry.FX;

namespace UberIRC {
	public class TextBox {
		public Rectangle MaxBounds;
		public Industry.FX.Font Font;
		public string    Text;
		public VerticalAlignment VerticalAlignment = VerticalAlignment.Top;
		public HorizontalAlignment HorizontalAlignment = HorizontalAlignment.Left;

		public void Backspace() { if ( Text.Length>0 ) Text = Text.Substring(0,Text.Length-1); }

		public Rectangle Bounds { get {
			var m = Font.MeasureLine(Text+" ").Bounds;
			var w = Math.Min( m.Width , MaxBounds.Width  );
			var h = Math.Min( m.Height, MaxBounds.Height );
			int x;
			int y;

			switch ( HorizontalAlignment ) {
			case HorizontalAlignment.Left:   x = MaxBounds.X; break;
			case HorizontalAlignment.Center: x = MaxBounds.X + (MaxBounds.Width-w)/2; break;
			case HorizontalAlignment.Right:  x = MaxBounds.X + MaxBounds.Width - w; break;
			default:                         throw new InvalidOperationException( "Invalid HorizontalAlignment " + HorizontalAlignment );
			}

			switch ( VerticalAlignment ) {
			case VerticalAlignment.Top:    y = MaxBounds.Y; break;
			case VerticalAlignment.Center: y = MaxBounds.Y + (MaxBounds.Height-h)/2; break;
			case VerticalAlignment.Bottom: y = MaxBounds.Y + MaxBounds.Height - h; break;
			default:                       throw new InvalidOperationException( "Invalid VerticalAlignment " + VerticalAlignment );
			}

			return new Rectangle( x, y, w, h );
		} }

		public void RenderTo( Graphics fx, bool cursor ) {
			Font.RenderLineTo(fx, Text+(cursor?"_":" "), Bounds, Bounds.Width == MaxBounds.Width ? HorizontalAlignment.Right : HorizontalAlignment, VerticalAlignment);
		}
	}
}
