// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;

namespace UberIRC.NET {
	class Error : Exception {
		public readonly string Code;
		public readonly string CodeName;

		public Error( string Code, string CodeName, string Message ): base(Message) {
			this.Code     = Code;
			this.CodeName = CodeName;
		}
	}

	class NicknameError : Error {
		public readonly string Nickname;

		public NicknameError( string Code, string CodeName, string Nickname, string Message ): base(Code,CodeName,Message) {
			this.Nickname = Nickname;
		}
	}

	class ServernameError : Error {
		public readonly string Server;

		public ServernameError( string Code, string CodeName, string Server, string Message ): base(Code,CodeName,Message) {
			this.Server = Server;
		}
	}

	class ChannelnameError : Error {
		public readonly string Channel;

		public ChannelnameError( string Code, string CodeName, string Channel, string Message ): base(Code,CodeName,Message) {
			this.Channel = Channel;
		}
	}

	class TargetError : Error {
		public readonly string Target;

		public TargetError( string Code, string CodeName, string Target, string Message ): base(Code,CodeName,Message) {
			this.Target = Target;
		}
	}

	class MaskError : Error {
		public readonly string Mask;

		public MaskError( string Code, string CodeName, string Mask, string Message ): base(Code,CodeName,Message) {
			this.Mask = Mask;
		}
	}

	class CommandError : Error {
		public readonly string Command;

		public CommandError( string Code, string CodeName, string Command, string Message ): base(Code,CodeName,Message) {
			this.Command = Command;
		}
	}

	class NicknameAndChannelError : Error {
		public readonly string Nickname;
		public readonly string Channel;

		public NicknameAndChannelError( string Code, string CodeName, string Nickname, string Channel, string Message ): base(Code,CodeName,Message) {
			this.Nickname = Nickname;
			this.Channel  = Channel;
		}
	}

	/*401*/ class NoSuchNickError          : NicknameError    { public NoSuchNickError         ( string nickname, string message ): base("401","ERR_NOSUCHNICK"      ,nickname,message) {} }
	/*402*/ class NoSuchServerError        : ServernameError  { public NoSuchServerError       ( string server  , string message ): base("402","ERR_NOSUCHSERVER"    ,server  ,message) {} }
	/*403*/ class NoSuchChannelError       : ChannelnameError { public NoSuchChannelError      ( string channel , string message ): base("403","ERR_NOSUCHCHANNEL"   ,channel ,message) {} }
	/*404*/ class CannotSendToChannelError : ChannelnameError { public CannotSendToChannelError( string channel , string message ): base("404","ERR_CANNOTSENDTOCHAN",channel ,message) {} }
	/*405*/ class TooManyChannelsError     : ChannelnameError { public TooManyChannelsError    ( string channel , string message ): base("405","ERR_TOOMANYCHANNELS" ,channel ,message) {} }
	/*406*/ class WasNoSuchNicknameError   : NicknameError    { public WasNoSuchNicknameError  ( string nickname, string message ): base("406","ERR_WASNOSUCHNICK"   ,nickname,message) {} }
	/*407*/ class TooManyTargetsError      : TargetError      { public TooManyTargetsError     ( string target  , string message ): base("407","ERR_TOOMANYTARGETS"  ,target  ,message) {} }
	/*409*/ class NoOriginError            : Error            { public NoOriginError           (                  string message ): base("409","ERR_NOORIGIN"        ,         message) {} }

	/*411*/ class NoRecipientError         : Error            { public NoRecipientError        (                  string message ): base("411","ERR_NORECIPIENT"     ,         message) {} }
	/*412*/ class NoTextToSendError        : Error            { public NoTextToSendError       (                  string message ): base("412","ERR_NOTEXTTOSEND"    ,         message) {} }
	/*413*/ class NoTopLevelError          : MaskError        { public NoTopLevelError         ( string mask    , string message ): base("413","ERR_NOTOPLEVEL"      ,mask    ,message) {} }
	/*414*/ class WildcardTopLevelError    : MaskError        { public WildcardTopLevelError   ( string mask    , string message ): base("414","ERR_WILDTOPLEVEL"    ,mask    ,message) {} }

	/*421*/ class UnknownCommandError      : CommandError     { public UnknownCommandError     ( string command , string message ): base("421","ERR_UNKNOWNCOMMAND"  ,command ,message) {} }
	/*422*/ class NoMOTDError              : Error            { public NoMOTDError             (                  string message ): base("422","ERR_NOMOTD"          ,         message) {} }
	/*423*/ class NoAdminInfoError         : ServernameError  { public NoAdminInfoError        ( string server  , string message ): base("423","ERR_NOADMININFO"     ,server  ,message) {} }
	/*424*/ class FileError                : Error            { public FileError               (                  string message ): base("424","ERR_FILEERROR"       ,         message) {} }

	/*431*/ class NoNicknameGivenError     : Error            { public NoNicknameGivenError    (                  string message ): base("431","ERR_NONICKNAMEGIVEN" ,         message) {} }
	/*432*/ class ErroneusNicknameError    : NicknameError    { public ErroneusNicknameError   ( string nickname, string message ): base("432","ERR_ERRONEUSNICKNAME",nickname,message) {} }
	/*433*/ class NicknameInUseError       : NicknameError    { public NicknameInUseError      ( string nickname, string message ): base("433","ERR_NICKNAMEINUSE"   ,nickname,message) {} }
	/*436*/ class NicknameCollisionError   : NicknameError    { public NicknameCollisionError  ( string nickname, string message ): base("436","ERR_NICKCOLLISION"   ,nickname,message) {} }
}
