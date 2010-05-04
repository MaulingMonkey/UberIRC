// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;

namespace Industry.FX {
	public partial class Font {
		int BitmapPageIndexFor( char ch ) {
			for ( int i = 0, len = Pages.Count; i < len; ++i )
			{
				Font.BitmapPage page = Pages[i];
				if ( page.Start <= ch )
				if ( page.End   >= ch )
				{
					return i;
				}
			}
			throw new IndexOutOfRangeException( "Character not found in font" );
		}

		int BitmapPageIndexForOrNul( ref char ch ) {
			try {
				return BitmapPageIndexFor(ch);
			} catch ( IndexOutOfRangeException ) {
				return BitmapPageIndexFor(ch='\0');
			}
		}

		Font.BitmapPage BitmapPageForOrNul( ref char ch ) {
			try {
				return BitmapPageFor(ch);
			} catch ( IndexOutOfRangeException ) {
				return BitmapPageFor(ch='\0');
			}
		}

		Font.BitmapPage BitmapPageFor( char ch ) {
			return Pages[BitmapPageIndexFor(ch)];
		}

		public IEnumerable<String> ToLines( string text, int maxwidth ) {
			int length = text.Length;

			int start = 0;
			while ( start < text.Length && text[start] == ' ' ) ++start;
			if ( start == length ) yield break;

			int end = text.IndexOf(' ',start+1);
			if ( end == -1 ) end = length;
			start = 0; // reset the start to avoid trimming leading whitespace

			for (;;) {
				bool createline = false;
				if ( end == length ) createline = true;

				if (!createline) {
					int possibleend = text.IndexOf(' ',end+1);
					if (possibleend == -1) possibleend = length;
					string possibleline = text.Substring( start, possibleend-start );
					if ( MeasureLine(possibleline).Bounds.Width > maxwidth ) {
						createline = true;
					} else {
						end = possibleend;
						continue;
					}
				}

				if (createline) {
					yield return text.Substring(start,end-start);
					if ( end == length ) yield break;

					start = end;
					while ( start < text.Length && text[start] == ' ' ) ++start;
					if ( start == length ) yield break;
					end = text.IndexOf(' ',start+1);
					if ( end == -1 ) end = length;
				}
			}
		}

		public static IEnumerable<TextRunLine> ToLines( Paragraph para, int maxwidth ) {
			int length = para.Length;

			var start = new ParagraphIterator() { Paragraph = para };
			if ( !start.TryFindAdvance((ch)=>ch!=' ') ) yield break;

			var end = start;
			end.TryFindAdvance((ch)=>ch==' ');
			start.Rewind();

			for (;;) {
				bool createline = false;
				if ( end.Run >= para.Runs ) createline = true;

				if (!createline) {
					var possibleend = end;
					possibleend.TryFindAdvance((ch)=>ch!=' ');
					possibleend.TryFindAdvance((ch)=>ch==' ');
					if ( MeasureLine(start,possibleend).Bounds.Width > maxwidth ) {
						createline = true;
					} else {
						end = possibleend;
						continue;
					}
				}

				if (createline) {
					yield return new TextRunLine(start,end);
					if ( end.Run >= para.Runs ) yield break;

					start = end;
					start.TryFindAdvance((ch)=>ch!=' ');
					if ( start.Run >= para.Runs ) yield break;
					end = start;
					end.TryFindAdvance((ch)=>ch==' ');
				}
			}
		}

		void Reload() {
			Pages = library.GetBitmapPageList( name, size );
		}
	}
}
