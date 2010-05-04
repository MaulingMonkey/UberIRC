// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using PaintDotNet;
using System.Drawing;

namespace Industry.FX {
	public static class PDN_FontLibraryExtensionMethods {
		static Regex reLayer      = new Regex(@"(.+) \((.+)\)"); // foo (bar)
		static Regex reSetting    = new Regex(@"([^:]+):(.+)"); // foo:bar
		static Regex reSizePx     = new Regex(@"(\d+)px"); // 123px
		static Regex reDimensions = new Regex(@"(\d+)x(\d+)@(\d+)x(\d+)-(\d+)x(\d+)"); // 1x2@3x4-5x6
		static Regex reRange      = new Regex(@"([a-fA-F0-9]+)-([a-fA-F0-9]+)"); // AB-CD

		public static void LoadPDNFile( this Font.Library library, string filename, Font.BitmapColorTransform ct ) {
			using ( var stream = new FileStream(filename,FileMode.Open,FileAccess.Read) ) library.LoadPDNStream(stream,ct);
		}
		public static void LoadPDNMemory( this Font.Library library, byte[] data, Font.BitmapColorTransform ct ) {
			using ( var stream = new MemoryStream(data) ) library.LoadPDNStream(stream,ct);
		}
		public static void LoadPDNStream( this Font.Library library, Stream stream, Font.BitmapColorTransform ct ) {
			using ( var document = Document.FromStream(stream) ) library.LoadPDNDocument(document,ct);
		}
		
		public static void LoadPDNDocument( this Font.Library library, Document document, Font.BitmapColorTransform ct ) {
			foreach ( var layer in document.Layers )
			if ( layer is BitmapLayer )
			{
				var blayer = layer as BitmapLayer;
				Match m = reLayer.Match(blayer.Name);
				if (!m.Success) continue; // skip nonparsable layers -- TODO:  Warning?

				string   fontname = m.Groups[1].Value;
				int      fontsize = 0;
				string[] settings = m.Groups[2].Value.Split(new char[]{' '});

				var page = new Font.BitmapPage();
				if ( ct != null ) page.ColorTransform = ct;
				
				foreach ( string setting in settings ) {
					m = reSetting.Match(setting);
					string name  = m.Groups[1].Value;
					string value = m.Groups[2].Value;

					switch ( name ) {
					case "size":
						m = reSizePx.Match(value);
						fontsize = int.Parse(m.Groups[1].Value);
						break;
					case "dimensions":
						m = reDimensions.Match(value);
						page.CharsWide  = int.Parse(m.Groups[1].Value);
						page.CharsTall  = int.Parse(m.Groups[2].Value);
						page.Measurement = new Font.Measurement();
						page.Measurement.Bounds  = new Size( int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value) );
						page.Measurement.Advance = new Point( page.CharWidth - int.Parse(m.Groups[5].Value), page.CharHeight - int.Parse(m.Groups[6].Value) );
						break;
					case "range":
						m = reRange.Match(value);
						page.Start = (char)int.Parse(m.Groups[1].Value,NumberStyles.HexNumber);
						page.End   = (char)int.Parse(m.Groups[2].Value,NumberStyles.HexNumber);
						break;
					default:
						throw new FileLoadException( "Unrecognized layer/page setting, "+setting );
					}
				}

				using ( var bitmap = blayer.Surface.CreateAliasedBitmap() ) page.Bitmap = new Bitmap(bitmap);
				library.Add( page, fontname, fontsize );
			}
		}
	}
}
