// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using Industry;
using Industry.FX;
using Color = System.Drawing.Color;
using UberIRC.Properties;

namespace UberIRC {
	public partial class IrcView {
		[Owns] Font.Library library;
		/*[Owns]*/ Font ltgray4, ltblue4, ltgray5, ltblue5, gray4, gray5, blue4, blue5, purple, black, red4, red5;

		// http://www.fileformat.info/info/unicode/block/miscellaneous_symbols/images.htm
		// http://www.fileformat.info/info/unicode/category/So/list.htm
		//
		// \u2601 Cloud              -- Emo Ra?
		// \u260E Black Telephone    -- PMs?
		// \u261B Black right point  -- PMs?
		// \u2620 Skull & Crossbones -- Pirate
		// \u262D Hammer & Sickle    -- Operator?
		// \u267F Wheelchair Symbol  -- Mentally handicapped
		// \u2699 Gear               -- Mode change?
		// \u26A0 Warning Sign /!\   -- Rules violation?

		Channel _currentView;
		Channel CurrentView {
			get {
				return _currentView;
			} set {
				_currentView = value;
				Invalidate();
				if ( _currentView != null ) Text = "UberIRC -- " + _currentView.ID.Channel;
				else                        Text = "UberIRC";
			}
		}
		TextStyle semiignore, normal, self, alerted, commanderror, system, smallalert;

		public void InitializeStyle() {
			library = new Font.Library();
			//library.LoadPDNMemory( Resources.UberConsole, Industry.FX.Font.GreyscaleAsForecolorAlphaScaledBitmapColorTransform );
			library.LoadUFF(@"I:\home\art\ui\uberconsole.uff1", Industry.FX.Font.GreyscaleAsForecolorAlphaScaledBitmapColorTransform );
			
			ltgray4 = new Font( library, "Uber Console", 4 ) { Color = Color.FromArgb(unchecked((int)0x44000000u)) };
			ltblue4 = new Font( library, "Uber Console", 4 ) { Color = Color.FromArgb(unchecked((int)0x440000BBu)) };
			ltgray5 = new Font( library, "Uber Console", 5 ) { Color = Color.FromArgb(unchecked((int)0x44000000u)) };
			ltblue5 = new Font( library, "Uber Console", 5 ) { Color = Color.FromArgb(unchecked((int)0x440000BBu)) };
			gray4   = new Font( library, "Uber Console", 4 ) { Color = Color.FromArgb(unchecked((int)0x88000000u)) };
			gray5   = new Font( library, "Uber Console", 5 ) { Color = Color.FromArgb(unchecked((int)0x88000000u)) };
			blue4   = new Font( library, "Uber Console", 4 ) { Color = Color.Blue   };
			blue5   = new Font( library, "Uber Console", 5 ) { Color = Color.Blue   };
			purple  = new Font( library, "Uber Console", 5 ) { Color = Color.Purple };
			black   = new Font( library, "Uber Console", 5 ) { Color = Color.Black  };
			red4    = new Font( library, "Uber Console", 4 ) { Color = Color.FromArgb(unchecked((int)0x88FF0000u)) };
			red5    = new Font( library, "Uber Console", 5 ) { Color = Color.Red    };
			
			semiignore = new TextStyle()
				{ Timestamp = new ColumnStyle() { Font = ltgray4, Width = 35 }
				, Nickname  = new ColumnStyle() { Font = ltgray4, Width = 100, HorizontalAlignment = HorizontalAlignment.Right }
				, Message   = new MessageColumnStyle() { Font = ltgray4, LinkFont = ltblue4, Width = -1 }
				};
			normal = new TextStyle()
				{ Timestamp = new ColumnStyle() { Font = gray4, Width = 35 }
				, Nickname  = new ColumnStyle() { Font = blue5, Width = 100, HorizontalAlignment = HorizontalAlignment.Right }
				, Message   = new MessageColumnStyle() { Font = black, LinkFont = blue5, Width = -1 }
				};
			self = new TextStyle()
				{ Timestamp = new ColumnStyle() { Font = gray4 , Width = 35 }
				, Nickname  = new ColumnStyle() { Font = purple, Width = 100, HorizontalAlignment = HorizontalAlignment.Right }
				, Message   = new MessageColumnStyle() { Font = black, LinkFont = blue5, Width = -1 }
				};
			alerted = new TextStyle()
				{ Timestamp = new ColumnStyle() { Font = gray4, Width = 35 }
				, Nickname  = new ColumnStyle() { Font = red5 , Width = 100, HorizontalAlignment = HorizontalAlignment.Right }
				, Message   = new MessageColumnStyle() { Font = black, LinkFont = blue5, Width = -1 }
				};
			commanderror = alerted;

			system = new TextStyle()
				{ Timestamp = new ColumnStyle() { Font = gray4, Width = 35 }
				, Nickname  = new ColumnStyle() { Font = gray5, Width = 100, HorizontalAlignment = HorizontalAlignment.Right }
				, Message   = new MessageColumnStyle() { Font = gray5, LinkFont = ltblue5, Width = -1 }
				};
			smallalert = new TextStyle()
				{ Timestamp = new ColumnStyle() { Font = red4, Width = 35 }
				, Nickname  = new ColumnStyle() { Font = red4, Width = 100, HorizontalAlignment = HorizontalAlignment.Right }
				, Message   = new MessageColumnStyle() { Font = red4, LinkFont = blue4, Width = -1 }
				};
		}
	}
}
