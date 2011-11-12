// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UberIRC.Providers {
	abstract class Provider {
		public Settings Settings;
		public IrcView  View;

		public virtual IEnumerable< KeyValuePair<String,Command> > Commands  { get { yield break; } }
		public virtual IEnumerable< KeyValuePair<Keys  ,Action > > Shortcuts { get { yield break; } }

		public virtual void OnChannelCreated( IrcView view, IrcView.Channel channel ) {}
	}
}
