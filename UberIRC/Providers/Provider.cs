// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;

namespace UberIRC.Providers {
	[AttributeUsage( AttributeTargets.Class )]
	class ProviderConfigAttribute : Attribute {
		public ProviderConfigAttribute( ) { }

		public bool Enabled = false;

		public static readonly ProviderConfigAttribute Default = new ProviderConfigAttribute( );

		public static ProviderConfigAttribute For( Type t ) {
			Debug.Assert( t.IsSubclassOf(typeof(Provider)) );
			var matching = t.GetCustomAttributes( typeof(ProviderConfigAttribute), true ).Cast<ProviderConfigAttribute>( ).ToArray( );
			Debug.Assert( matching.Length <= 1 );
			return matching.Length > 0 ? matching[0] : Default;
		}
	}

	abstract class Provider {
		IrcView _view;

		public virtual Settings Settings { get; set; }
		public virtual IrcView  View { get {
			return _view;
		} set {
			if( _view == value ) return;
			if( _view != null ) OnIrcViewDisconnected(_view);
			_view = value;
			if( _view != null ) OnIrcViewConnected(_view);
		}}

		public virtual IEnumerable< KeyValuePair<String,Command> > Commands  { get { yield break; } }
		public virtual IEnumerable< KeyValuePair<Keys  ,Action > > Shortcuts { get { yield break; } }

		public virtual void OnIrcViewConnected   ( IrcView view ) { var el = this as NET.IEventListener; if( el != null ) view.IrcListeners.Add(el); }
		public virtual void OnIrcViewDisconnected( IrcView view ) { var el = this as NET.IEventListener; if( el != null ) view.IrcListeners.Remove(el); }
		public virtual void OnChannelCreated( IrcView view, IrcView.Channel channel ) {}
	}
}
