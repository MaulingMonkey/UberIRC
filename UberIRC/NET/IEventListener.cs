// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using Actor = UberIRC.NET.Irc.Actor;

namespace UberIRC.NET {
	public interface IEventListener {
		void OnRawRecv          ( IrcConnection connection, string rawrecv );

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

		void OnRplInvited       ( IrcConnection connection, Actor who, string channel ); // 341
		void OnErrNickInUse     ( IrcConnection connection, string nick ); // 433
		void OnErrNotChannelOp  ( IrcConnection connection, string channel, string message ); // 482

		void OnRecvParseError   ( IrcConnection connection, string rawrecv, Exception e );
		void OnConnectionError  ( IrcConnection connection, Exception e );
	}

	public class BasicEventListener : IEventListener {
		public virtual void OnRawRecv          ( IrcConnection connection, string rawline ) {}

		public virtual void OnJoin             ( IrcConnection connection, Actor who, string channel ) {}
		public virtual void OnPart             ( IrcConnection connection, Actor who, string channel ) {}
		public virtual void OnQuit             ( IrcConnection connection, Actor who, string channel, string message ) {}
		public virtual void OnPrivMsg          ( IrcConnection connection, Actor who, string target , string message ) {}
		public virtual void OnNotice           ( IrcConnection connection, Actor who, string target , string message ) {}
		public virtual void OnNick             ( IrcConnection connection, Actor who, string channel, string newnick ) {}

		public virtual void OnTopic            ( IrcConnection connection, Actor op , string channel, string topic   ) {}
		public virtual void OnKick             ( IrcConnection connection, Actor op , string channel, string target, string message ) {}
		public virtual void OnModeChange       ( IrcConnection connection, Actor op , string channel, string mode, string target ) {}
		public virtual void OnChannelModeChange( IrcConnection connection, Actor op , string channel, string mode, string param ) {}

		public virtual void OnRplInvited       ( IrcConnection connection, Actor who, string channel ) {} // 341
		public virtual void OnErrNickInUse     ( IrcConnection connection, string nick ) {} // 433
		public virtual void OnErrNotChannelOp  ( IrcConnection connection, string channel, string message ) {} // 482

		public virtual void OnRecvParseError   ( IrcConnection connection, string rawrecv, Exception e ) {}
		public virtual void OnConnectionError  ( IrcConnection connection, Exception e ) {}
	}
}
