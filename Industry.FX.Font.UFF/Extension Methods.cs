using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Industry.FX {
	[Serializable] class UFF1 {
		[Serializable] public class Page {
			public string FontName;
			public int    FontSize;

			public char First, Last; // inclusive range
			public int CharsWide, CharsTall;
			public byte[] Bitmap;
			public int AdvanceX, AdvanceY;
			public int GlyphWidth, GlyphHeight;
		}
		public readonly List<Page> Pages = new List<Page>();
	}

	public static class Font_UFF_ExtensionMethods {
		public static void SaveUFF1( this IEnumerable<Font> fonts, string filename ) {
			var uff1 = new UFF1();

			foreach ( var font in fonts ) {
				var pages = font._XXX_GetPages();
				foreach ( var page in pages ) {
					var uff1_page = new UFF1.Page()
						{ AdvanceX    = page.AdvanceX
						, AdvanceY    = page.AdvanceY
						, Bitmap      = null
						, CharsTall   = page.CharsTall
						, CharsWide   = page.CharsWide
						, First       = page.Start
						, FontName    = font.Name
						, FontSize    = font.Size
						, Last        = page.End
						, GlyphWidth  = page.CharWidth
						, GlyphHeight = page.CharHeight
						};

					using ( var ms = new MemoryStream() ) {
						page.Bitmap.Save( ms, ImageFormat.Png );
						uff1_page.Bitmap = ms.ToArray();
					}

					uff1.Pages.Add( uff1_page );
				}
			}

			var s = new BinaryFormatter();
			using ( var stream = File.OpenWrite(filename) ) s.Serialize( stream, uff1 );
		}

		public static void SaveUFF( this IEnumerable<Font> fonts, string filename ) { SaveUFF1(fonts,filename); }

		public static void SaveUFF ( Font font, string filename ) { new[]{font}.SaveUFF (filename); }
		public static void SaveUFF1( Font font, string filename ) { new[]{font}.SaveUFF1(filename); }

		static bool LoadUFF1( this Font.Library library, string filename, Font.BitmapColorTransform ct ) {
			var s = new BinaryFormatter();
			UFF1 uff1;
			using ( var stream = File.OpenRead(filename) ) uff1 = s.Deserialize(stream) as UFF1;
			if ( uff1==null ) return false;

			foreach ( var page in uff1.Pages ) {
				using ( var page_bitmap = new MemoryStream(page.Bitmap) )
				{
					var bmpage = new Font.BitmapPage()
						{ Bitmap = (Bitmap)Bitmap.FromStream(page_bitmap)
						, CharsTall = page.CharsTall
						, CharsWide = page.CharsWide
						, End = page.Last
						, Start = page.First
						, Measurement = new Font.Measurement()
							{ Advance = new Point( page.AdvanceX  , page.AdvanceY    )
							, Bounds  = new Size ( page.GlyphWidth, page.GlyphHeight )
							}
						};
					if ( ct!=null ) bmpage.ColorTransform = ct;
					library.Add( bmpage, page.FontName, page.FontSize );
				}
			}
			return true;
		}

		public static void LoadUFF( this Font.Library library, string filename, Font.BitmapColorTransform ct ) {
			if ( library.LoadUFF1(filename,ct) ) return;
			throw new ArgumentException( string.Format( "{0} is not a UFF1 file", filename ) );
		}
	}
}
