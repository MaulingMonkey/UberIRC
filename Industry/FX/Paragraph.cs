// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using GDI = System.Drawing;
using GDII = System.Drawing.Imaging;

namespace Industry.FX {
	public struct TextRun {
		public Font   Font;
		public string Text;
		public object Tag;
	}

	public struct SequencedTextRun {
		public int BeginOffset, EndOffset;
		public TextRun Run;
	}

	public class TextRunLine : List<TextRun> {
		public TextRunLine( ParagraphIterator begin, ParagraphIterator end ): base(end.Run-begin.Run+(end.Character==0?0:1)) {
			var firstrun = begin.Paragraph.Run(begin.Run);
			int len = end.Run-begin.Run+(end.Character==0?0:1);

			if ( begin.Run == end.Run ) {
				Add( new TextRun()
					{ Font = firstrun.Run.Font
					, Text = firstrun.Run.Text.Substring( begin.Character, end.Character-begin.Character )
					, Tag  = firstrun.Run.Tag
					});
				return;
			}
			
			if ( begin.Character != 0 ) {
				Add( new TextRun()
					{ Font = firstrun.Run.Font
					, Text = firstrun.Run.Text.Substring( begin.Character )
					, Tag  = firstrun.Run.Tag
					});
			}
			int ibeg  = begin.Character==0 ? 0 : 1 ;
			int iend  = end.Character==0 ? len : len-1 ;
			for ( int i = ibeg ; i < iend ; ++i ) {
				Add( begin.Paragraph.Run(i+begin.Run).Run );
			}
			if ( end.Character != 0 ) {
				var lastrun  = begin.Paragraph.Run(end.Run);
				Add( new TextRun()
					{ Font = lastrun.Run.Font
					, Text = lastrun.Run.Text.Substring(0,end.Character)
					, Tag  = lastrun.Run.Tag
					});
			}
		}
	}

	public class Paragraph : IEnumerable<SequencedTextRun> {
		List<SequencedTextRun> runs = new List<SequencedTextRun>();
		public int Length { get; private set; }

		public int Runs { get { return runs.Count; } }
		public SequencedTextRun Run(int index) { return runs[index]; }

		public Paragraph() {}
		public Paragraph( IEnumerable<TextRun> runs ) { foreach ( var run in runs ) Add(run); }

		public void Add( TextRun run ) {
			int begoffset = Length;
			int endoffset = Length = begoffset + run.Text.Length;
			runs.Add( new SequencedTextRun() { BeginOffset = begoffset, EndOffset = endoffset, Run = run } );
		}

		public IEnumerable<TextRunLine> ToLines( int maxwidth ) {
			return Font.ToLines(this,maxwidth);
		}

		public IEnumerator<SequencedTextRun> GetEnumerator() { return runs.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	public partial class Font {
		public static void RenderLinesTo( GDI.Graphics fx, IEnumerable<TextRunLine> lines, GDI.Rectangle dest, HorizontalAlignment halign, VerticalAlignment valign ) {
			if ( dest.Width <= 0 || dest.Height <= 0 ) return;
		    using ( var bitmap = new GDI.Bitmap( dest.Width, dest.Height ) ) {
		        using ( var fx2 = GDI.Graphics.FromImage(bitmap) ) fx2.Clear( GDI.Color.Transparent );
		        RenderLinesTo( bitmap, lines, new GDI.Rectangle(0,0,dest.Width,dest.Height), halign, valign );
		        fx.DrawImage( bitmap, dest.X, dest.Y );
		    }
		}

		public static void RenderLinesTo( GDI.Bitmap bm, IEnumerable<TextRunLine> lines, GDI.Rectangle dest, HorizontalAlignment halign, VerticalAlignment valign ) {
			if ( dest.Width <= 0 || dest.Height <= 0 ) return;
		    var bits = bm.LockBits( dest, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
		    RenderLinesTo( bits, lines, new GDI.Rectangle(0,0,dest.Width,dest.Height), halign, valign );
		    bm.UnlockBits(bits);
		}

		public static void RenderLinesTo( GDII.BitmapData bm, IEnumerable<TextRunLine> lines, GDI.Rectangle dest, HorizontalAlignment halign, VerticalAlignment valign ) {
			if ( dest.Width <= 0 || dest.Height <= 0 ) return;
			Font.Measurement m = Font.MeasureLines(lines);
			
			int y;
			switch ( valign ) {
			case VerticalAlignment.Top:    y = dest.Top; break;
			case VerticalAlignment.Center: y = dest.Top + (dest.Height-m.Bounds.Height)/2; break;
			case VerticalAlignment.Bottom: y = dest.Bottom - m.Bounds.Height; break;
			default: throw new ArgumentException("Invalid VerticalAlignment: "+valign, "valign");
			}

			foreach ( var line in lines ) {
				Font.Measurement linem = Font.MeasureLine(line);
				int x;
				switch ( halign ) {
				case HorizontalAlignment.Left:   x = dest.Left; break;
				case HorizontalAlignment.Center: x = dest.Left + (dest.Width-linem.Bounds.Width)/2; break;
				case HorizontalAlignment.Right:  x = dest.Right - linem.Bounds.Width; break;
				default: throw new ArgumentException("Invalid HorizontalAlignment: "+halign, "halign");
				}

				foreach ( var run in line ) {
					run.Font.RenderLineTo( bm, run.Text, new GDI.Rectangle(x,y,dest.Right-x,dest.Bottom-y), HorizontalAlignment.Left, VerticalAlignment.Top );
					x += run.Font.MeasureLine( run.Text ).Advance.X;
				}

				y += linem.Advance.Y;
			}
		}
	}

	public struct ParagraphIterator {
		public Paragraph Paragraph;
		public int Run;
		public int Character;

		public bool TryFindAdvance( Func<char,bool> predicate ) {
			for ( ; Run < Paragraph.Runs ; Character = 0, ++Run ) {
				for ( var text = Paragraph.Run(Run).Run.Text ; Character < text.Length ; ++Character ) {
					if (predicate(text[Character])) return true;
				}
			}

			return false;
		}

		public static bool operator<( ParagraphIterator lhs, ParagraphIterator rhs ) {
			Debug.Assert( lhs.Paragraph == rhs.Paragraph );
			return (lhs.Run!=rhs.Run) ? lhs.Run < rhs.Run
				: (lhs.Character!=rhs.Character) ? lhs.Character < rhs.Character
				: false
				;
		}

		public static bool operator>( ParagraphIterator lhs, ParagraphIterator rhs ) {
			return rhs < lhs;
		}

		public void Next() {
			if ( Run < Paragraph.Runs )
			if ( ++Character >= Paragraph.Run(Run).Run.Text.Length )
			{
				++Run;
				Character = 0;
			}
		}

		public void Rewind() { Run = Character = 0; }
	}
}
