// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using SlimDX.Multimedia;
using SlimDX.XAudio2;

namespace UberIRC {
	public class Sound {
		internal static readonly XAudio2        XAudio2;
		internal static readonly MasteringVoice Master;

		static Sound() {
			XAudio2 = new XAudio2();
			Master  = new MasteringVoice(XAudio2,2);
		}

		readonly string Name;

		internal Sound( string name ) {
			Name = name;
		}

		void Play( params float[] balances ) {
		}

		readonly Dictionary<string,float[]> DisplayBalances = new Dictionary<string,float[]>()
			{ { @"\\.\DISPLAY1", new[]{ 1.5f, 1.0f } }
			, { @"\\.\DISPLAY2", new[]{ 1.5f, 0.0f } }
			, { @"\\.\DISPLAY3", new[]{ 0.0f, 1.5f } }
			};

		public void Play( Form on ) {
			var screens = Screen.AllScreens;
			var screens_left  = screens.Min( screen => screen.Bounds.Left  );
			var screens_right = screens.Max( screen => screen.Bounds.Right );
			var screens_width = screens_right-screens_left;

			var bestScreen = screens.OrderByDescending( screen => {
				var area = screen.Bounds;
				area.Intersect( on.Bounds );
				return area.Width*area.Height;
			}).First();

			var balances = new[]{1.5f,1.5f};
			if ( screens.Length==3 && DisplayBalances.ContainsKey(bestScreen.DeviceName) ) balances = DisplayBalances[bestScreen.DeviceName];

			var path   = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\"+Name+@"\.Current").GetValue(null) as string;
			var stream = new WaveStream(path);
			var buffer = new AudioBuffer() { AudioBytes=(int)stream.Length, AudioData=stream, Flags=BufferFlags.EndOfStream };

			var voice = new SourceVoice( XAudio2, stream.Format );
			voice.SubmitSourceBuffer( buffer );
			voice.SetChannelVolumes( balances.Length, balances );
			voice.BufferEnd += (sender,ctx) => {
				try {
					on.BeginInvoke(new Action(()=>{
						voice.Dispose();
						buffer.Dispose();
						stream.Dispose();
					}));
				} catch ( InvalidOperationException ) {
					// herp derp on must be disposed/gone
				}
			};
			voice.Start();
		}
	}

	public static class Sounds {
		public static readonly Sound
			Beep = new Sound(".Default");

		public static void DisposeOfXAudio2() {
			using ( Sound.Master  ) {}
			using ( Sound.XAudio2 ) {}
		}
	}
}
