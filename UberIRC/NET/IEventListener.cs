// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using Actor = UberIRC.NET.Irc.Actor;

namespace UberIRC.NET {
	public interface IEventListener {
		void OnJoin             ( IrcConnection connection, Actor who, string channel );
		void OnPart             ( IrcConnection connection, Actor who, string channel );
		void OnQuit             ( IrcConnection connection, Actor who, string channel, string message );
		void OnPrivMsg          ( IrcConnection connection, Actor who, string target , string message );
		void OnNotice           ( IrcConnection connection, Actor who, string target , string message );
		void OnNick             ( IrcConnection connection, Actor who, string channel, string newnick );
		
		void OnTopic            ( IrcConnection connection, Actor op , string channel, string topic   );
		void OnKick             ( IrcConnection connection, Actor op , string channel, string target, string message );
		void OnModeChange       ( IrcConnection connection, Actor op , string channel, string mode, string target );
		void OnChannelModeChange( IrcConnection connection, Actor op , string channel, string mode, string param );

		void OnRecvParseError   ( IrcConnection connection, string rawrecv, Exception e );
		void OnConnectionError  ( IrcConnection connection, Exception e );
	}
}
