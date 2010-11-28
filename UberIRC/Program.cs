// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using UberIRC.Properties;
using Industry.FX;
using System.Linq;

namespace UberIRC {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			var SettingsPath = Path.Combine( Application.UserAppDataPath, "settings.xml" );
			if (!File.Exists(SettingsPath))
			using ( var writer = File.Create(SettingsPath,Resources.DefaultSettings.Length,FileOptions.SequentialScan) )
			{
				var b = Resources.DefaultSettings;
				writer.Write(b,0,b.Length);
			}
#if DEBUG
			Process.Start( Application.UserAppDataPath );
			var DebugSettingsPath = Path.Combine( Application.UserAppDataPath, "debug-settings.xml" );
			if ( File.Exists(DebugSettingsPath) ) SettingsPath = DebugSettingsPath;
#endif
			Settings settings = new Settings(SettingsPath);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			try {
				using ( var view = new IrcView(settings) ) Application.Run(view);
			} finally {
				Sounds.DisposeOfXAudio2();
			}
		}
	}
}

