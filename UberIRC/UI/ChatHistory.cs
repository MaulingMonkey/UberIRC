// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Industry;
using Industry.FX;
using Font = Industry.FX.Font;

namespace UberIRC {
	public struct MessageColumnStyle {
		public int                 Width;
		public Font                Font;
		public Font                LinkFont;
		public HorizontalAlignment HorizontalAlignment;
		public VerticalAlignment   VerticalAlignment  ;
	}

	public struct ColumnStyle {
		public int                 Width; // pixels
		public Font                Font;
		public HorizontalAlignment HorizontalAlignment;
		public VerticalAlignment   VerticalAlignment  ;

		public static implicit operator ColumnStyle( MessageColumnStyle mcs ) {
			return new ColumnStyle()
				{ Width = mcs.Width
				, Font  = mcs.Font
				, HorizontalAlignment = mcs.HorizontalAlignment
				, VerticalAlignment   = mcs.VerticalAlignment
				};
		}
	}

	public class TextStyle {
		public ColumnStyle Timestamp;
		public int TimestampNicknameMargin = 4;
		public ColumnStyle Nickname;
		public int NicknameMessageMargin = 4;
		public MessageColumnStyle Message;
	}

	public struct HistoryEntry {
		public TextStyle Style;

		public String    Timestamp;
		public String    Nickname;
		public TextRun[] Message;
	}

	public class ChatHistory : RAII {
		class Entry {
			public HistoryEntry Base;
			public Bitmap       RenderCache;
			public int          Advance, Height;

			public object PickTagAt( int x, int y ) {
				x -= (Base.Style.Timestamp.Width + Base.Style.TimestampNicknameMargin + Base.Style.Nickname.Width + Base.Style.NicknameMessageMargin);
				var message = ItemizeLinesOf(Base.Style.Message, Base.Message );
				return Font.PickTagAt( message, x, y );
			}
		}

		Rectangle bounds;

		void Invalidate() {
			foreach ( var entry in History ) DisposeOf( ref entry.RenderCache );
		}

		void DisposeOf<T>( ref T value ) where T : class, IDisposable {
			if ( value != null ) value.Dispose();
			value = null;
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
				bounds.Height = value;
			}
		}

		List<Entry> History;
		int CurrentIndex = -1;

		public ChatHistory() {
			History = new List<Entry>();
		}

		public void Add( HistoryEntry history ) {
			if (CurrentIndex == History.Count-1) CurrentIndex = History.Count;
			for ( int i = 0 ; i < history.Message.Length ; ++i ) history.Message[i].Text = history.Message[i].Text.Replace("\t","    ");
			History.Add( new Entry() { Base = history } );
		}

		public void PageUp() { CurrentIndex = Math.Max(0,CurrentIndex-10); }
		public void PageDown() { CurrentIndex = Math.Min(History.Count-1,CurrentIndex+10); }

		void UpdateLineCache( int index ) {
			Entry extentry = History[index];
			if ( extentry.RenderCache != null ) return; // already up to date
			HistoryEntry entry = History[index].Base;

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

			var measure_timestamp = entry.Style.Timestamp.Font.MeasureLines(timestamp);
			var measure_nickname  = entry.Style.Nickname .Font.MeasureLines(nickname );
			var measure_message   =                       Font.MeasureLines(message  );

			var measurements = new[] { measure_timestamp, measure_nickname, measure_message };
			int height   = measurements.Select(m=>m.Bounds.Height).Max();
			int advancey = measurements.Select(m=>m.Advance.Y    ).Max();
			
			extentry.RenderCache = new Bitmap(Bounds.Width,height,PixelFormat.Format32bppArgb);
			using ( var fx = Graphics.FromImage(extentry.RenderCache) ) {
				int x = 0, y = 0;
				RenderColumnTo( fx, new Point(x,y), height, entry.Style.Timestamp, timestamp ); x += entry.Style.Timestamp.Width; x += entry.Style.TimestampNicknameMargin;
				RenderColumnTo( fx, new Point(x,y), height, entry.Style.Nickname , nickname  ); x += entry.Style.Nickname .Width; x += entry.Style.NicknameMessageMargin;
				RenderColumnTo( fx, new Point(x,y), height, entry.Style.Message  , message   ); x += entry.Style.Message  .Width;
			}
			extentry.Advance = advancey;
			extentry.Height  = height;
		}
		public void RenderTo( Graphics fx ) {
			var start = CurrentIndex;
			var end   = -1;

			if ( start == -1 ) return;
			Debug.Assert( start>end ); // bottom to top my friend!

			var dest = Bounds;
			int y = dest.Bottom-1;
			int i;
			for ( i = History.Count-1 ; i > start ; --i ) DisposeOf( ref History[i].RenderCache );
			for ( i = start ; y>=0 && i > end ; --i ) {
				Entry     extentry = History[i];
				HistoryEntry entry = History[i].Base;

				UpdateLineCache(i);
				int height   = extentry.Height;
				int advancey = extentry.Advance;
				if ( i == CurrentIndex ) advancey = height;
				y -= advancey;

				int x = dest.Left;
				fx.DrawImage( extentry.RenderCache, new Point(x,y) );
			}
			for ( ; i > end ; --i ) DisposeOf( ref History[i].RenderCache );
		}
		public void ClickAt( int x, int y ) {
			if ( CurrentIndex == -1 ) return;

			int yr = Bounds.Bottom-1;
			for ( int i = CurrentIndex ; yr >= 0 && i > -1 ; --i ) {
				Entry     extentry = History[i];
				HistoryEntry entry = History[i].Base;

				UpdateLineCache(i);
				int height   = extentry.Height;
				int advancey = extentry.Advance;
				if ( i == CurrentIndex ) advancey = height;
				yr -= advancey;

				object tag = extentry.PickTagAt(x-0,y-yr);
				Action action;
				if ( null != (action = tag as Action) ) action();
			}
		}

		public static IEnumerable<String> ItemizeLinesOf( ColumnStyle style, string text ) {
			return style.Font.ToLines( text, style.Width );
		}
		public static IEnumerable<TextRunLine> ItemizeLinesOf( ColumnStyle style, TextRun[] text ) {
			return new Paragraph(text).ToLines(style.Width);
		}

		public void RenderColumnTo( Graphics fx, Point position, int height, ColumnStyle style, IEnumerable<String> text ) {
			int x = position.X;
			int y = position.Y;

			foreach ( var line in text ) {
				style.Font.RenderLineTo(fx, line, new Rectangle(x,y,style.Width,height), style.HorizontalAlignment, style.VerticalAlignment);
				y += style.Font.MeasureLine(line).Advance.Y;
			}
		}
		public void RenderColumnTo( Graphics fx, Point position, int height, ColumnStyle style, IEnumerable<TextRunLine> text ) {
			Font.RenderLinesTo( fx, text, new Rectangle(position.X,position.Y,style.Width,height), style.HorizontalAlignment, style.VerticalAlignment );
		}
	}
}
