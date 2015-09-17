// Copyright Michael B. E. Rickert 2012
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using System.Linq;
using UberIRC.NET;
using System.Net.Sockets;

namespace UberIRC.Providers
{
	class CachedHashedCsvFile : CachedFile
	{
		HashSet<string> _Lines = new HashSet<string>();
		public HashSet<string> Lines { get { return _Lines; }}

		public CachedHashedCsvFile( string local, string remote ): base( local, remote ) {}

		protected override void OnDownloadComplete()
		{
			if(!File.Exists(LocalPath)) return;
			var lines = new HashSet<string>(File.ReadAllLines(LocalPath));
			_Lines = lines;
		}
	}

	[ProviderConfig( Enabled=false )]
	class TorDetectionProvider : Provider, NET.IEventListener
	{
		static string LocalTorCsvPath { get { return Path.Combine(Application.UserAppDataPath,"tor.csv"); }}
		static string WebTorCsvPath { get { return @"http://torstatus.blutmagie.de/ip_list_all.php/Tor_ip_list_ALL.csv"; }}
		static readonly CachedHashedCsvFile TorCsv = new CachedHashedCsvFile(LocalTorCsvPath,WebTorCsvPath);
		static readonly Dictionary<string,string> DnsCache = new Dictionary<string,string>();

		void CheckForTor( Irc.Actor actor ) {
			lock( DnsCache ) {
				if( DnsCache.ContainsKey(actor.Hostname) && TorCsv.Lines.Contains(DnsCache[actor.Hostname]) ) {
					// ...tor detected...
				}
			}
		}

		void OnHostnameResolve( IAsyncResult iar ) {
			try {
				var resolve = Dns.EndResolve(iar);
				var nuh = (Irc.Actor)iar.AsyncState;
				lock( DnsCache ) {
					var ipv4 = resolve.AddressList.FirstOrDefault(addr=>addr.AddressFamily == AddressFamily.InterNetwork); // IPv4 please

					if( ipv4 != null )
						DnsCache.Add( nuh.Hostname, ipv4.ToString() );

					Debug.WriteLineIf
						( resolve.AddressList.Count(addr=>addr.AddressFamily == AddressFamily.InterNetwork)>=2
						, "WARNING: Multiple IPv4 addresses for hostname entry"
						);
				}
			} catch( Exception e ) {
				View.OnProviderError( "tor detection provider dns resolution", e );
			}
		}

		public override void OnIrcViewConnected( IrcView view )
		{
			base.OnIrcViewConnected( view );
		}

		public override void OnChannelCreated( IrcView view, IrcView.Channel channel )
		{
			base.OnChannelCreated( view, channel );
		}

		// IEventListener
		public void OnRawRecv( IrcConnection connection, string rawline ){}
		public void OnJoin( IrcConnection connection, Irc.Actor who, string channel )
		{
			lock( DnsCache )
			{
				if( DnsCache.ContainsKey(who.Hostname) )
					Dns.BeginResolve( who.Hostname, OnHostnameResolve, who );
				// ...
			}
		}
		public void OnPart( IrcConnection connection, Irc.Actor who, string channel ){}
		public void OnQuit( IrcConnection connection, Irc.Actor who, string channel, string message ){}
		public void OnPrivMsg( IrcConnection connection, Irc.Actor who, string target, string message ){}
		public void OnNotice( IrcConnection connection, Irc.Actor who, string target, string message ){}
		public void OnNick( IrcConnection connection, Irc.Actor who, string channel, string newnick ){}
		public void OnTopic( IrcConnection connection, Irc.Actor op, string channel, string topic ){}
		public void OnKick( IrcConnection connection, Irc.Actor op, string channel, string target, string message ){}
		public void OnModeChange( IrcConnection connection, Irc.Actor op, string channel, string mode, string target ){}
		public void OnChannelModeChange( IrcConnection connection, Irc.Actor op, string channel, string mode, string param ){}
		public void OnRplInvited( IrcConnection connection, Irc.Actor who, string channel ){}
		public void OnErrNickInUse( IrcConnection connection, string nick ){}
		public void OnErrNotChannelOp( IrcConnection connection, string channel, string message ){}
		public void OnRecvParseError( IrcConnection connection, string rawrecv, Exception e ){}
		public void OnConnectionError( IrcConnection connection, Exception e ){}
	}
}
