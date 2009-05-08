// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;

namespace UberIRC.Providers {
	class PasteProvider : Provider {
		public override IEnumerable< KeyValuePair<Keys,Command> > Shortcuts { get {
			
			//<paste shortcut="Ctrl+Shift+V">
			//    <text lang="C#"  to="http://gamedev.pastebin.com/pastebin.php" post="paste=Send&poster={poster}&expiry=m&format=csharp&code2={code}" />
			//    <text lang="C++" to="http://gamedev.pastebin.com/pastebin.php" post="paste=Send&poster={poster}&expiry=m&format=cpp&code2={code}" />
			//    <text            to="http://gamedev.pastebin.com/pastebin.php" post="paste=Send&poster={poster}&expiry=m&format=csharp&code2={code}" />
			//    <image           to="http://load.imageshack.us/" post="fileupload={image}" />
			//</paste>

			yield break;

			foreach ( XmlNode node in Settings.SelectNodes("//paste") ) {
			}
		} }
	}
}
