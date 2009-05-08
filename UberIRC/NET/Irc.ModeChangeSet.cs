// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UberIRC.NET {
	public partial class Irc {
		public class ModeChangeSet {
			List<char>   TakenModeTypes  = new List<char>();
			List<string> TakenModeParams = new List<string>();
			List<char>   AddedModeTypes  = new List<char>();
			List<string> AddedModeParams = new List<string>();

			public IEnumerable< KeyValuePair<string,string> > TakenModes { get {
				Debug.Assert( TakenModeTypes.Count == TakenModeParams.Count );
				for ( int i = 0, len = TakenModeTypes.Count; i < len ; ++i ) {
					yield return new KeyValuePair<string,string>("-"+TakenModeTypes[i],TakenModeParams[i]);
				}
			} }

			public IEnumerable< KeyValuePair<string,string> > AddedModes { get {
				Debug.Assert( AddedModeTypes.Count == AddedModeParams.Count );
				for ( int i = 0, len = AddedModeTypes.Count; i < len ; ++i ) {
					yield return new KeyValuePair<string,string>("+"+AddedModeTypes[i],AddedModeParams[i]);
				}
			} }

			public IEnumerable< KeyValuePair<string,string> > AllModes { get {
				Debug.Assert( TakenModeTypes.Count == TakenModeParams.Count );
				Debug.Assert( AddedModeTypes.Count == AddedModeParams.Count );
				for ( int i = 0, len = TakenModeTypes.Count; i < len ; ++i ) {
					yield return new KeyValuePair<string,string>("-"+TakenModeTypes[i],TakenModeParams[i]);
				}
				for ( int i = 0, len = AddedModeTypes.Count; i < len ; ++i ) {
					yield return new KeyValuePair<string,string>("+"+AddedModeTypes[i],AddedModeParams[i]);
				}
			} }

			HashSet<char> UserModeTypes    = new HashSet<char>() {'O','o','h','v','b','e','I'};
			HashSet<char> ChannelModeTypes = new HashSet<char>() {'a','i','m','n','q','p','s','r','t','k','l'};
			public IEnumerable< KeyValuePair<string,string> > UserModes { get { return GetModes(UserModeTypes); } }
			public IEnumerable< KeyValuePair<string,string> > ChannelModes { get { return GetModes(ChannelModeTypes); } }

			IEnumerable< KeyValuePair<string,string> > GetModes( HashSet<char> types ) {
				Debug.Assert( TakenModeTypes.Count == TakenModeParams.Count );
				Debug.Assert( AddedModeTypes.Count == AddedModeParams.Count );

				for ( int i = 0, len = TakenModeTypes.Count; i < len ; ++i )
				if ( types.Contains(TakenModeTypes[i]) )
				{
					yield return new KeyValuePair<string,string>("-"+TakenModeTypes[i],TakenModeParams[i]);
				}

				for ( int i = 0, len = AddedModeTypes.Count; i < len ; ++i )
				if ( types.Contains(AddedModeTypes[i]) )
				{
					yield return new KeyValuePair<string,string>("+"+AddedModeTypes[i],AddedModeParams[i]);
				}
			}

			public ModeChangeSet( string input ) {
				string[] p = input.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);

				int i = 0;
				while ( i < p.Length ) {
					string command = p[i++];
					char addsubmode = '\0';

					foreach ( var ch in command )
					switch (ch)
					{
						// IRC Modes documentation:
						// http://www.mirc.com/help/rfc2811.txt
					case '+':
					case '-':
						addsubmode = ch;
						break;
					case 'O': // user has channel creator
					case 'o': // user has channel operator
					case 'h': // user has channel half operator
					case 'v': // user has channel voice
					case 'b': // ban mask
					case 'e': // ban exception mask
					case 'I': // invite mask
						// single parameter modes
						switch ( addsubmode ) {
						case '+':
							AddedModeTypes .Add(ch);
							AddedModeParams.Add( p[i++] );
							break;
						case '-':
							TakenModeTypes .Add(ch);
							TakenModeParams.Add( p[i++] );
							break;
						default:
							throw new FormatException( "Invalid mode string format -- expected + or - leading mode string" );
						}
						break;
					case 'a': // channel is anonymous flag
					case 'i': // channel is invite-only flag
					case 'm': // channel is moderated flag
					case 'n': // channel forbids outside messages flag
					case 'q': // channel is quiet flag
					case 'p': // channel is private flag
					case 's': // channel is secret flag
					case 'r': // channel reops flag
					case 't': // channel topic is by ops only flag
						// no parameters
						switch ( addsubmode ) {
						case '+':
							AddedModeTypes .Add(ch);
							AddedModeParams.Add( null );
							break;
						case '-':
							TakenModeTypes .Add(ch);
							TakenModeParams.Add( null );
							break;
						default:
							throw new FormatException( "Invalid mode string format -- expected + or - leading mode string" );
						}
						break;
					case 'k':
					case 'l':
						// single parameter when setting only modes
						switch ( addsubmode ) {
						case '+':
							AddedModeTypes .Add(ch);
							AddedModeParams.Add( p[i++] );
							break;
						case '-':
							TakenModeTypes .Add(ch);
							TakenModeParams.Add( null );
							break;
						default:
							throw new FormatException( "Invalid mode string format -- expected + or - leading mode string" );
						}
						break;
					}
				}
			}
		}
	}
}
