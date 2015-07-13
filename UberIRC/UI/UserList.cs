// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System.Drawing;
using System.Linq;
using System.Drawing.Imaging;
using System;

namespace UberIRC {
	public class UserList {
		public IrcView.Channel SelectedChannel;
		public Rectangle Bounds;

		Bitmap Cache;
		string[] CachedNickList;

		void UpdateCache() {
			var nicks = SelectedChannel.ID.Connection.WhosIn(SelectedChannel.ID.Channel).OrderBy( cui => cui.Nick ).OrderBy( cui => "@+% ".IndexOf( cui.Sigil ) ).Select( cui => cui.Sigil + cui.Nick ).ToArray();
			if ( CachedNickList!=null && nicks.SequenceEqual(CachedNickList) ) return;

			using ( Cache ) Cache = null;
			CachedNickList = nicks;
			if ( nicks.Length==0 ) return;

			var measurements = nicks.Select(nick=>Normal.MeasureLine(nick));
			var h = measurements.Sum(m=>m.Advance.Y);
			var w = measurements.Max(m=>m.Bounds.Width);

			Cache = new Bitmap( w, h, PixelFormat.Format32bppArgb );

			int y=0;
			using ( var fx = Graphics.FromImage(Cache) ) {
				fx.Clear( Color.Transparent );
				foreach ( var nick in nicks ) {
					var font = Normal;
					var m = font.MeasureLine(nick);
					font.RenderLineTo( fx, nick, new Rectangle(0,y,w,h), Industry.FX.HorizontalAlignment.Left, Industry.FX.VerticalAlignment.Top );
					y += m.Advance.Y;
				}
			}
		}

		public Size RequestedSize { get {
			UpdateCache();
			return (Cache==null) ? Size.Empty : Cache.Size;
		}}

		public Industry.FX.Font Normal;

		public void RenderTo( Graphics fx ) {
			UpdateCache();
			if ( Cache == null ) return;

			var w = Math.Min( Bounds.Width , Cache.Width );
			var h = Math.Min( Bounds.Height, Cache.Height );

			fx.DrawImage( Cache, new Rectangle( Bounds.Left, Bounds.Top, w, h ), new Rectangle( 0, 0, w, h ), GraphicsUnit.Pixel );
		}
	}
}
