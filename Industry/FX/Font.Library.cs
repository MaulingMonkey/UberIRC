// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System.Collections.Generic;

namespace Industry.FX {
	public partial class Font {
		public partial class Library : RAII {
			struct EntryKey {
				public string Name;
				public int    Size;
			}

			[Owns] Dictionary< EntryKey, Entry > entries = new Dictionary<EntryKey,Entry>();

			internal List<BitmapPage> GetBitmapPageList( string name, int size ) {
				return entries[ new EntryKey() { Name = name, Size = size } ].BitmapPages;
			}

			public void Add( BitmapPage page, string fontname, int fontsize ) {
				var key = new EntryKey() { Name = fontname, Size = fontsize };
				if ( !entries.ContainsKey(key) ) entries.Add( key, new Entry() { FontName = fontname, FontSize = fontsize } );
				var entry = entries[key];
				entry.BitmapPages.Add( page );
			}
		}
	}
}
