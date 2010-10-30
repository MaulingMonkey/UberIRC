using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UberIRC.NET;

namespace UberIRC {
	public partial class IrcView {
		public partial class Channel {
			public readonly List<Filter> NuhFilters = new List<Filter>();
		}

		public struct Filter {
			public string    Pattern;
			public Regex     Regex;
			public TextStyle Style;
		}

		public TextStyle GetStyleFor( Channel view, IrcConnection connection, Irc.Actor who, string target, string message ) {
			var style
				= connection.ActualNickname == who.Nickname     ? self
				: message.Contains( connection.ActualNickname ) ? alerted
				: normal
				;

			var nuh = who.ToString();

			lock ( view.NuhFilters ) foreach ( var filter in view.NuhFilters ) if ( filter.Regex.IsMatch(nuh) ) style = filter.Style;

			return style;
		}

		static Regex RegexFromPattern( string pattern ) {
			Regex re;

			string prefix
				= pattern.Contains("@")                                      ? ""
				: !pattern.ToLower().ToCharArray().Any(ch=>'a'<=ch&&ch<='z') ? @"^(.+?)\!(.+?)@"
				: @"^(.+?)\!(.+?)@(.*?)"
				;

			if ( pattern.StartsWith("/") && pattern.EndsWith("/") ) {
				re = new Regex( prefix + pattern.Substring(1,pattern.Length-2).Replace(@"\/","/"),RegexOptions.Compiled);
			} else if ( pattern.StartsWith("/") && pattern.EndsWith("/i") ) {
				re = new Regex( prefix + pattern.Substring(1,pattern.Length-3).Replace(@"\/","/"),RegexOptions.Compiled|RegexOptions.IgnoreCase);
			} else if ( pattern.StartsWith("\"") && pattern.EndsWith("\"") ) {
				re = new Regex( prefix + Regex.Escape(pattern.Substring(1,pattern.Length-2)), RegexOptions.IgnoreCase );
			} else {
				bool contains_alpha = pattern.ToLower().ToCharArray().Any(ch=>'a'<=ch&&ch<='z');
				re = new Regex
					( prefix + Regex.Escape(pattern).Replace(@"\*",".*?")
					, RegexOptions.IgnoreCase
					);
			}

			return re;
		}

		private void AddPattern( string desc, string pattern, TextStyle style ) {
			Begin(()=>{
				var view = CurrentView;
				if ( view == null ) return;

				lock ( view.NuhFilters ) if ( pattern == "" ) {
					bool first = true;
					foreach ( var filter in view.NuhFilters.Where(f=>f.Style==style) ) {
						AddHistory( view, first?(desc+"ing"):"", Timestamp, filter.Pattern, system );
						first = false;
					}
					if ( first ) AddHistory( view, desc+"ing", Timestamp, "absolutely nobody", system );
				} else {
					view.NuhFilters.Add( new Filter() { Pattern=pattern, Regex=RegexFromPattern(pattern), Style=style } );
					AddHistory( view, "", Timestamp, "You are now "+desc+"ing "+pattern, system );
				}
			});
		}

		private void RemovePattern( string desc, string pattern, params TextStyle[] styles ) {
			Begin(()=>{
				var view = CurrentView;
				if ( view == null ) return;

				lock ( view.NuhFilters ) if ( view.NuhFilters.RemoveAll(f=>f.Pattern==pattern&&styles.Any(s=>f.Style==s))>0 ) {
					AddHistory( CurrentView, "", Timestamp, "You have un"+desc+"ed "+pattern, system );
				} else {
					AddHistory( CurrentView, "ERROR", Timestamp, "You are not "+desc+"ing "+pattern, commanderror );
				}
			});
		}

		public void Ignore    ( string pattern ) { AddPattern("ignor",pattern,null); }
		public void SemiIgnore( string pattern ) { AddPattern("semiignor",pattern,semiignore); }
		public void Baddy     ( string pattern ) { AddPattern("twitlist",pattern,baddyalert); }
		public void UnIgnore( string pattern ) { RemovePattern("ignor"   ,pattern,null,semiignore); }
		public void UnBaddy(  string pattern ) { RemovePattern("twitlist",pattern,baddyalert); }
	}
}
