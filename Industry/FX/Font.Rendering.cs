// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Industry.FX {
	public partial class Font {
		public void RenderLineTo( Graphics fx, string text, Rectangle bounds, HorizontalAlignment halign, VerticalAlignment valign ) {
			if ( bounds.Width <= 0 || bounds.Height <= 0 ) return;

			using ( var bitmap = new Bitmap( bounds.Width, bounds.Height ) ) {
				using ( var fx2 = Graphics.FromImage(bitmap) ) fx2.Clear( Color.Transparent );
				RenderLineTo( bitmap, text, new Rectangle(0,0,bounds.Width,bounds.Height), halign, valign );
				fx.DrawImage( bitmap, bounds.X, bounds.Y );
			}
		}

		public void RenderLineTo( Bitmap bm, string text, Rectangle bounds, HorizontalAlignment halign, VerticalAlignment valign ) {
			if ( bounds.Width <= 0 || bounds.Height <= 0 ) return;

			var bits = bm.LockBits( bounds, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
			RenderLineTo( bits, text, new Rectangle(0,0,bounds.Width,bounds.Height), halign, valign );
			bm.UnlockBits(bits);
		}

		unsafe public void RenderLineTo( BitmapData bm, string text, Rectangle bounds, HorizontalAlignment halign, VerticalAlignment valign ) {
			if ( bounds.Width <= 0 || bounds.Height <= 0 ) return;

			Measurement measurement = MeasureLine(text);

			int destx;
			switch ( halign ) {
			case HorizontalAlignment.Left:   destx = bounds.Left; break;
			case HorizontalAlignment.Center: destx = bounds.Left + (bounds.Width-measurement.Bounds.Width)/2; break;
			case HorizontalAlignment.Right:  destx = bounds.Right - measurement.Bounds.Width; break;
			default: throw new ArgumentException( "Invalid HorizontalAlignment: "+halign, "halign" );
			}

			int desty;
			switch ( valign ) {
			case VerticalAlignment.Top:    desty = bounds.Top; break;
			case VerticalAlignment.Center: desty = bounds.Top + (bounds.Height-measurement.Bounds.Height)/2; break;
			case VerticalAlignment.Bottom: desty = bounds.Bottom - measurement.Bounds.Height; break;
			default: throw new ArgumentException( "Invalid VerticalAlignment: "+valign, "valign" );
			}

			List<BitmapData> pagebms = new List<BitmapData>(Pages.Count);
			Font.BitmapPage page = null;

			for ( int i = 0, length = text.Length ; i < length && !(destx>bounds.Right); ++i, destx += page.AdvanceX ) {
				char ch = text[i];
				int pageindex;
				try {
					pageindex = BitmapPageIndexFor(ch);
				} catch ( IndexOutOfRangeException ) {
					pageindex = BitmapPageIndexFor(ch='\0');
				}
				page = Pages[pageindex];
				
				while (pagebms.Count<=pageindex) pagebms.Add(null);
				if (pagebms[pageindex] == null) {
					var rect = new Rectangle( new Point(0,0), page.Bitmap.Size );
					pagebms[pageindex] = page.Bitmap.LockBits( rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb );
				}
				var pagebm = pagebms[pageindex];
				int index = ch-page.Start;

				Blit
					( pagebm
					, bm
					, new Rectangle
						( index%page.CharsWide * page.CharWidth
						, index/page.CharsWide * page.CharHeight
						, Math.Min(page.CharWidth ,bounds.Right-destx)
						, Math.Min(page.CharHeight,bounds.Bottom-desty)
						)
					, new Point( destx, desty )
					, page.ColorTransform
					);
			}

			for ( int i = 0, len = pagebms.Count ; i < len ; ++i ) {
				if ( pagebms[i] == null ) continue;
				Pages[i].Bitmap.UnlockBits(pagebms[i]);
			}
		}

		unsafe void Blit( BitmapData from, BitmapData dest, Rectangle fromarea, Point topos, Font.BitmapColorTransform transform ) {
			int fromx = fromarea.X;
			int fromy = fromarea.Y;
			int destx = topos.X;
			int desty = topos.Y;
			int width = fromarea.Width;
			int height = fromarea.Height;

			if ( destx < 0 ) {
				width -= -destx;
				fromx += -destx;
				destx = 0;
			}

			if ( desty < 0 ) {
				height -= -desty;
				fromy  += -desty;
				desty = 0;
			}

			width  = Math.Min( width , from.Width -fromx );
			height = Math.Min( height, from.Height-fromy );
			width  = Math.Min( width , dest.Width -destx );
			height = Math.Min( height, dest.Height-desty );

			if ( width  <= 0 ) return;
			if ( height <= 0 ) return;

			var from_scan0 = (byte*)from.Scan0.ToPointer();
			var dest_scan0 = (byte*)dest.Scan0.ToPointer();

			for ( int yo = 0 ; yo < height ; ++yo ) {
				int* destp = destx + (int*)( dest.Stride*(yo+desty) + dest_scan0 );
				int* fromp = fromx + (int*)( from.Stride*(yo+fromy) + from_scan0 );

				for ( int xo = 0 ; xo < width ; ++xo ) {
					Color towrite = transform( Color.FromArgb(*fromp++), Color, Color.FromArgb(*destp) );
					*destp++ = towrite.ToArgb();
				}
			}
		}
	}
}
