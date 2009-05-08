// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System.Runtime.InteropServices;

namespace UberIRC {
	public partial class IrcView {
		[DllImport("user32.dll")]
		static extern void MessageBeep(uint uType); 
    
		const uint MB_OK                = 0x00000000;
	    
		const uint MB_ICONHAND          = 0x00000010;
		const uint MB_ICONQUESTION      = 0x00000020;
		const uint MB_ICONEXCLAMATION   = 0x00000030;
		const uint MB_ICONASTERISK      = 0x00000040;
	}
}
