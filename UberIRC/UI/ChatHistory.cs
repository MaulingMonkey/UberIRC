// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Industry;
using Industry.FX;
using Font = Industry.FX.Font;

namespace UberIRC {
	public struct ColumnStyle {
		public int                 Width; // pixels
		public Font                Font;
		public HorizontalAlignment HorizontalAlignment;
		public VerticalAlignment   VerticalAlignment  ;
	}

	public class TextStyle {
		public ColumnStyle Timestamp;
		public int TimestampNicknameMargin = 4;
		public ColumnStyle Nickname;
		public int NicknameMessageMargin = 4;
		public ColumnStyle Message;
	}

	public struct HistoryEntry {
		public TextStyle Style;

		public String Timestamp;
		public String Nickname;
		public String Message;
	}

	public class ChatHistory : RAII {
		Rectangle bounds;
		[Owns] Bitmap    front, back;
		void SwapFrontBack() {
			Bitmap t = front;
			front = back;
			back = t;
		}

		void Invalidate() {
			if ( front != null ) front.Dispose();
			if ( back  != null ) back.Dispose();
			front = back = null;
		}

		public Rectangle Bounds {
			get {
				return bounds;
			} set {
				X = value.X;
				Y = value.Y;
				Width  = value.Width;
				Height = value.Height;
			}
		}

		public int X { get { return bounds.X; } set { bounds.X = value; } }
		public int Y { get { return bounds.Y; } set { bounds.Y = value; } }

		public int Width {
			get {
				return bounds.Width;
			} set {
				if ( bounds.Width != value ) Invalidate();
				bounds.Width = value;
			}
		}

		public int Height {
			get {
				return bounds.Height;
			} set {
				if ( front != null && front.Height < value ) Invalidate();
				bounds.Height = value;
			}
		}

		List<HistoryEntry> History;
		int FrontIndex   = -1;
		int CurrentIndex = -1;

		public ChatHistory() {
			History = new List<HistoryEntry>();
		}

		public void Add( HistoryEntry history ) {
			if (CurrentIndex == History.Count-1) CurrentIndex = History.Count;
			history.Message = history.Message.Replace("\t","    ");
			History.Add(history);
		}

		public void PageUp() { CurrentIndex = Math.Max(0,CurrentIndex-10); }
		public void PageDown() { CurrentIndex = Math.Min(History.Count-1,CurrentIndex+10); }

		public void RenderTo( Graphics target ) {
			if ( front != null ) {
				if ( FrontIndex != CurrentIndex ) {
					if ( back == null ) back = new Bitmap(front.Width,front.Height);
					RerenderScrolled( back, front, FrontIndex );
					SwapFrontBack();
				}
			} else {
				front = new Bitmap(bounds.Width,bounds.Height);
				using ( var fx = Graphics.FromImage(front) ) RenderLinesTo( fx, CurrentIndex, -1 );
			}

			target.DrawImage( front, new Point(-1,Height-front.Height) );
			FrontIndex = CurrentIndex;
		}
		void RenderLinesTo( Graphics fx, int start, int end ) {
			if ( start == -1 ) return;
			Debug.Assert( start>end ); // bottom to top my friend!

			var dest = Bounds;
			int y = dest.Bottom-1;
			for ( int i = start ; y>=0 && i > end ; --i ) {
				HistoryEntry entry = History[i];

				entry.Style.Message.Width
					= Width
					- entry.Style.Timestamp.Width
					- entry.Style.TimestampNicknameMargin
					- entry.Style.Nickname.Width
					- entry.Style.NicknameMessageMargin
					;

				var timestamp = ItemizeLinesOf(entry.Style.Timestamp,entry.Timestamp);
				var nickname  = ItemizeLinesOf(entry.Style.Nickname ,entry.Nickname );
				var message   = ItemizeLinesOf(entry.Style.Message  ,entry.Message  );

				int height   = 0;
				int advancey = 0;
				height = Math.Max(height,entry.Style.Timestamp.Font.MeasureLines(timestamp).Bounds.Height);
				height = Math.Max(height,entry.Style.Nickname .Font.MeasureLines(nickname ).Bounds.Height);
				height = Math.Max(height,entry.Style.Message  .Font.MeasureLines(message  ).Bounds.Height);
				advancey = Math.Max(advancey,entry.Style.Timestamp.Font.MeasureLines(timestamp).Advance.Y);
				advancey = Math.Max(advancey,entry.Style.Nickname .Font.MeasureLines(nickname ).Advance.Y);
				advancey = Math.Max(advancey,entry.Style.Message  .Font.MeasureLines(message  ).Advance.Y);
				if ( i == CurrentIndex ) advancey = height;
				y -= advancey;

				int x = dest.Left;
				RenderColumnTo( fx, new Point(x,y), height, entry.Style.Timestamp, timestamp ); x += entry.Style.Timestamp.Width; x += entry.Style.TimestampNicknameMargin;
				RenderColumnTo( fx, new Point(x,y), height, entry.Style.Nickname , nickname  ); x += entry.Style.Nickname .Width; x += entry.Style.NicknameMessageMargin;
				RenderColumnTo( fx, new Point(x,y), height, entry.Style.Message  , message   ); x += entry.Style.Message  .Width;
			}
		}

		public void RerenderScrolled( Bitmap target, Bitmap original, int oldindex ) {
			// target is assumed to have cached version

			Debug.Assert( target.Size == original.Size );

			int dy = 0;
			int indexdir = oldindex<CurrentIndex ? +1 : -1;
			int minindex = Math.Min(oldindex,CurrentIndex);
			int maxindex = Math.Max(oldindex,CurrentIndex);

			for ( int i = maxindex ; i != minindex ; --i ) { // [max..min) index advances summed into dy
				HistoryEntry entry = History[i];

				entry.Style.Message.Width
					= Width
					- entry.Style.Timestamp.Width
					- entry.Style.TimestampNicknameMargin
					- entry.Style.Nickname.Width
					- entry.Style.NicknameMessageMargin
					;

				var timestamp = ItemizeLinesOf(entry.Style.Timestamp,entry.Timestamp);
				var nickname  = ItemizeLinesOf(entry.Style.Nickname ,entry.Nickname );
				var message   = ItemizeLinesOf(entry.Style.Message  ,entry.Message  );

				int height   = 0;
				int advancey = 0;
				height = Math.Max(height,entry.Style.Timestamp.Font.MeasureLines(timestamp).Bounds.Height);
				height = Math.Max(height,entry.Style.Nickname .Font.MeasureLines(nickname ).Bounds.Height);
				height = Math.Max(height,entry.Style.Message  .Font.MeasureLines(message  ).Bounds.Height);
				advancey = Math.Max(advancey,entry.Style.Timestamp.Font.MeasureLines(timestamp).Advance.Y);
				advancey = Math.Max(advancey,entry.Style.Nickname .Font.MeasureLines(nickname ).Advance.Y);
				advancey = Math.Max(advancey,entry.Style.Message  .Font.MeasureLines(message  ).Advance.Y);

				dy += indexdir*advancey;
			}

			Rectangle rect = (dy>0) ? new Rectangle( 0, target.Height-dy, target.Width, dy ) : new Rectangle( 0, 0, target.Width, -dy );

			using ( var fx = Graphics.FromImage(target) ) {
				fx.Clear( Color.Transparent );
				fx.DrawImageUnscaledAndClipped( original, new Rectangle(0,-dy,front.Width,front.Height) );
				fx.SetClip( rect );
				if ( dy>0 ) RenderLinesTo( fx, maxindex, minindex ); // scrolled down, only draw bottom lines
				else        RenderLinesTo( fx, CurrentIndex, -1   ); // scrolled up, draw everything
			}
		}

		public IEnumerable<String> ItemizeLinesOf( ColumnStyle style, string text ) {
			return style.Font.ToLines( text, style.Width );
		}

		public void RenderColumnTo( Graphics fx, Point position, int height, ColumnStyle style, IEnumerable<String> text ) {
			int x = position.X;
			int y = position.Y;

			foreach ( var line in text ) {
				style.Font.RenderLineTo(fx, line, new Rectangle(x,y,style.Width,height), style.HorizontalAlignment, style.VerticalAlignment);
				y += style.Font.MeasureLine(line).Advance.Y;
			}
		}
	}
}
