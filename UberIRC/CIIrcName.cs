
namespace UberIRC {
	/// <summary>
	/// Case Insensitive IRC Name
	/// </summary>
	public struct CIIrcName {
		private string Data;

		public static implicit operator CIIrcName( string s ) { return new CIIrcName() { Data=s }; }
		public static implicit operator string( CIIrcName n ) { return n.Data; }

		public string ToLower() { return (Data??"").ToLowerInvariant().Replace('{','[').Replace('}',']').Replace('|','\\'); } // Silly scandinavians :<

		public static bool operator==( CIIrcName lhs, CIIrcName rhs ) { return lhs.ToLower() == rhs.ToLower(); }
		public static bool operator!=( CIIrcName lhs, CIIrcName rhs ) { return lhs.ToLower() != rhs.ToLower(); }

		public override bool Equals( object obj ) { return obj is CIIrcName && (CIIrcName)obj == this; }
		public override int GetHashCode() { return ToLower().GetHashCode(); }
		public override string ToString() { return Data; }
	}
}
