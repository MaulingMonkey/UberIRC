// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Drawing;
using System.Windows.Forms;
using Industry;
using Industry.FX;
using FX = Industry.FX;

namespace HelloPDN {
	public partial class Form1 : Form, IDisposable {
		public Form1() {
			InitializeComponent();

			Library.LoadPDNMemory( HelloPDN.Properties.Resources.UberConsole, FX.Font.GreyscaleAsForecolorBitmapColorTransform );
			RedFont  = new Industry.FX.Font( Library, "Uber Console", 5 ) { Color = Color.Red };
			BlueFont = new Industry.FX.Font( Library, "Uber Console", 5 ) { Color = Color.Blue };
		}

		void IDisposable.Dispose() { RAII.Dispose(this); }

		[Owns] FX.Font.Library Library = new FX.Font.Library();
		FX.Font RedFont, BlueFont;

		int frames = 0;
		int framedisplay = 0;
		DateTime prev = DateTime.Now;
		private void Form1_Paint(object sender, PaintEventArgs e) {
			++frames;

			var fx = e.Graphics;
			using ( var buffer = new Bitmap( ClientSize.Width, ClientSize.Height ) ) {
				using ( var clr = Graphics.FromImage(buffer) ) clr.Clear( Color.Transparent );
				var target = buffer.LockBits( ClientRectangle, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

				int y = 0;
				while ( y < ClientSize.Height ) {
#if false
					string text = "Hello, World!";
					Font.RenderLineTo( target, text, new Rectangle(0,y,ClientSize.Width,ClientSize.Height-y), Industry.FX.HorizontalAlignment.Center, VerticalAlignment.Top );
					y += Font.MeasureLine(text).Advance.Y;
#else
					Paragraph text = new Paragraph();
					text.Add( new TextRun() { Font = RedFont , Text = "He" } );
					text.Add( new TextRun() { Font = BlueFont, Text = "llo, W" } );
					text.Add( new TextRun() { Font = RedFont , Text = "orld!" } );
					var lines = text.ToLines(ClientSize.Width);
					FX.Font.RenderLinesTo( target, lines, new Rectangle(0,y,ClientSize.Width,int.MaxValue), FX.HorizontalAlignment.Center, VerticalAlignment.Top );
					y += FX.Font.MeasureLines(lines).Advance.Y;
#endif
				}

				DateTime now = DateTime.Now;
				var span = now-prev;
				if ( span.TotalSeconds >= 1.0 ) {
					prev = now;
					framedisplay = frames;
					frames = 0;
				}

				RedFont.RenderLineTo( target, "FPS: "+framedisplay, ClientRectangle, Industry.FX.HorizontalAlignment.Right, VerticalAlignment.Top );
				buffer.UnlockBits(target);
				fx.DrawImage(buffer,0,0);
			}
			Invalidate();
		}
	}
}
