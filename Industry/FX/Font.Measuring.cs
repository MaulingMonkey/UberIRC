// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Industry.FX {
	public partial class Font {
		public struct Measurement {
			public Size  Bounds;
			public Point Advance;

			public static Measurement Merge( Measurement lhs, int xoff, int yoff, Measurement rhs ) {
				return new Measurement()
					{ Bounds = new Size()
						{ Width  = Math.Max( lhs.Bounds.Width , xoff+rhs.Bounds.Width  )
						, Height = Math.Max( lhs.Bounds.Height, yoff+rhs.Bounds.Height )
						}
					, Advance = new Point()
						{ X = Math.Max( lhs.Advance.X, xoff+rhs.Advance.X )
						, Y = Math.Max( lhs.Advance.Y, yoff+rhs.Advance.Y )
						}
					};
			}
			public static Measurement MergeLtoR( Measurement left, Measurement right ) { return Merge( left, left.Advance.X, 0, right ); }
			public static Measurement MergeTtoB( Measurement top, Measurement bottom ) { return Merge( top, 0, top.Advance.Y, bottom ); }
		}

		public Measurement MeasureLine( String line ) {
			Measurement m = new Measurement();
			foreach ( char ch_ in line ) {
				char ch = ch_;
				m = Measurement.Merge(m,m.Advance.X,0,BitmapPageForOrNul( ref ch ).Measurement);
			}
			return m;
		}

		public static Measurement MeasureLine( TextRunLine line ) {
			Measurement m = new Measurement();

			foreach ( var run in line )
			foreach ( var ch_ in run.Text )
			{
				char ch = ch_;
				m = Measurement.Merge(m,m.Advance.X,0,run.Font.BitmapPageForOrNul( ref ch ).Measurement);
			}

			return m;
		}

		public static object PickTagAt( TextRunLine line, int x, int y ) {
			Measurement m = new Measurement();

			foreach ( var run in line )
			foreach ( var ch_ in run.Text )
			{
				char ch = ch_;
				m = Measurement.Merge(m,m.Advance.X,0,run.Font.BitmapPageForOrNul( ref ch ).Measurement );
				if ( 0 <= x && x < m.Bounds.Width && 0 <= y && y < m.Bounds.Height ) return run.Tag;
			}
			return null;
		}

		public static Measurement MeasureLine( ParagraphIterator begin, ParagraphIterator end ) {
			var m = new Measurement();

			for ( ParagraphIterator i = begin ; i < end ; i.Next() ) {
				var run = i.Paragraph.Run(i.Run).Run;
				char ch = run.Text[i.Character];
				BitmapPage page = run.Font.BitmapPageForOrNul( ref ch );
				m = Measurement.Merge(m,m.Advance.X,0,page.Measurement);
			}

			return m;
		}

		public Measurement MeasureLines( IEnumerable<String> text ) {
			Measurement m = new Measurement();
			foreach ( var line in text ) m = Measurement.Merge(m,0,m.Advance.Y,MeasureLine(line));
			return m;
		}

		public static Measurement MeasureLines( IEnumerable<TextRunLine> text ) {
			Measurement m = new Measurement();
			foreach ( var line in text ) m = Measurement.Merge(m,0,m.Advance.Y,MeasureLine(line));
			return m;
		}

		public static object PickTagAt( IEnumerable<TextRunLine> text, int x, int y ) {
			Measurement m = new Measurement();

			foreach ( var line in text ) {
				foreach ( var run in line )
				foreach ( var ch_ in run.Text )
				{
					char ch = ch_;
					m = Measurement.Merge(m,m.Advance.X,0,run.Font.BitmapPageForOrNul( ref ch ).Measurement );
					if ( 0 <= x && x < m.Bounds.Width && 0 <= y && y < m.Bounds.Height ) return run.Tag;
				}
				y -= m.Advance.Y;
				m = new Measurement();
			}
			return null;
		}
	}
}
