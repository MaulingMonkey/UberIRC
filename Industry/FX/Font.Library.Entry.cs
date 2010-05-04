// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System.Collections.Generic;

namespace Industry.FX {
	public partial class Font {
		public partial class Library {
			public class Entry : RAII {
				public string                  FontName;
				public int                     FontSize;
				[Owns] public List<BitmapPage> BitmapPages = new List<BitmapPage>();
			}
		}
	}
}
