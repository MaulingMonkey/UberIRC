// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System.Drawing;
using System.Collections.Generic;

namespace Industry.FX {
	public partial class Font {
		/// <summary>
		/// Transformation function applied per-pixel during rendering
		/// </summary>
		/// <param name="origin">Pixel color value from the BitmapPage</param>
		/// <param name="foreground">"Foreground" color value from the Font instance to replace appropriate color keys from origin</param>
		/// <param name="background">"Background" color value from the render target to mix against or replace color keys from origin</param>
		/// <returns>The resulting color to replace the current render target's pixel color</returns>
		public delegate Color BitmapColorTransform( Color origin, Color foreground, Color background );


		/// <summary>
		/// Default color transformation function -- completely ignores foreground and directly renders the font
		/// </summary>
		public static BitmapColorTransform DefaultBitmapColorTransform { get { return (o,fg,bg) => Color.FromArgb
			( 0xFF
			, (o.R * o.A + bg.R * (255-o.A)) / 255
			, (o.G * o.A + bg.G * (255-o.A)) / 255
			, (o.B * o.A + bg.B * (255-o.A)) / 255
			);
		}}

		/// <summary>
		/// Transforms (255,N,N,N) -- that is, opaque greyscale -- to the foreground color (with alpha multiplied by (255-N)/255), and directly renders non-greyscale
		/// </summary>
		public static BitmapColorTransform GreyscaleAsForecolorBitmapColorTransform { get { return (o,fg,bg) => {
			bool bw = (o.R==o.G && o.G==o.B && o.A==255);
			int alpha = bw?(255-o.R):o.A;
			if ( alpha == 0 ) return bg;
			Color c   = bw?fg:o;
			byte resulta = (byte)((c.A * alpha + bg.A * (255-alpha)) / 255);
			Color result = Color.FromArgb
				( resulta
				, (c.R * alpha + bg.R * (resulta-alpha)) / resulta
				, (c.G * alpha + bg.G * (resulta-alpha)) / resulta
				, (c.B * alpha + bg.B * (resulta-alpha)) / resulta
				);
			return result;
		};}}

		/// <summary>
		/// Transforms opaque greyscale to the foreground color alpha-modulated, and everything else to itself adjusted by the foreground alpha
		/// </summary>
		public static BitmapColorTransform GreyscaleAsForecolorAlphaScaledBitmapColorTransform { get { return (o,fg,bg) => {
			bool bw = (o.R==o.G && o.G==o.B && o.A==255);
			
			Color c = bw ? Color.FromArgb((255-o.R)*fg.A/255,fg.R,fg.G,fg.B) : Color.FromArgb(o.A*fg.A/255, o );
			int alpha = c.A;
			if ( alpha == 0 ) return bg;
			byte resulta = (byte)((o.A * alpha + bg.A * (255-alpha)) / 255);
			Color result = Color.FromArgb
				( resulta
				, (c.R * alpha + bg.R * (resulta-alpha)) / resulta
				, (c.G * alpha + bg.G * (resulta-alpha)) / resulta
				, (c.B * alpha + bg.B * (resulta-alpha)) / resulta
				);
			return result;
		};}}

		public class BitmapPage : RAII {
			public char Start, End; // inclusive range

			public BitmapColorTransform ColorTransform = DefaultBitmapColorTransform; // default algorithm ignores fg entirely

			public int CharsWide, CharsTall;
			//public int CharWidth, CharHeight;
			//public int AdvanceX , AdvanceY;
			public Measurement Measurement;
			public int CharWidth  { get { return Measurement.Bounds.Width;  } }
			public int CharHeight { get { return Measurement.Bounds.Height; } }
			public int AdvanceX   { get { return Measurement.Advance.X;     } }
			public int AdvanceY   { get { return Measurement.Advance.Y;     } }

			[Owns] public Bitmap Bitmap;
		}
	}
}
