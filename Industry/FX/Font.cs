// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System.Collections.Generic;
using System.Drawing;

namespace Industry.FX {
	public partial class Font {
		Font.Library          library;
		List<Font.BitmapPage> Pages;
		string name;
		int    size;

		public Color  Color = Color.Black;
		public string Name { get { return name; } set { if (name == value) return; name = value; Reload(); } }
		public int    Size { get { return size; } set { if (size == value) return; size = value; Reload(); } }

		public Font( Font.Library library, string name, int size ) {
			this.library = library;
			this.name = name;
			this.size = size;
			Reload();
		}
	}
}
