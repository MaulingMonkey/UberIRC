// Copyright Michael B. E. Rickert 2009-2010
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
using System.Xml;

namespace UberIRC.Providers {
	class PasteProvider : Provider {
		//<paste shortcut="Ctrl+Shift+V">
		//    <text lang="C#"  to="http://gamedev.pastebin.com/pastebin.php" post="paste=Send poster={poster} expiry=m format=csharp code2={code}" scrape="&lt;li class=&quot;highlight&quot;&gt;&lt;a href=&quot;(.+)&quot;&gt;.+&lt;/a&gt;&lt;br/&gt;\d+ sec ago&lt;/li&gt;" />
		//    <text lang="C++" to="http://gamedev.pastebin.com/pastebin.php" post="paste=Send poster={poster} expiry=m format=cpp    code2={code}" scrape="&lt;li class=&quot;highlight&quot;&gt;&lt;a href=&quot;(.+)&quot;&gt;.+&lt;/a&gt;&lt;br/&gt;\d+ sec ago&lt;/li&gt;" />
		//    <text lang="XML" to="http://gamedev.pastebin.com/pastebin.php" post="paste=Send poster={poster} expiry=m format=xml    code2={code}" scrape="&lt;li class=&quot;highlight&quot;&gt;&lt;a href=&quot;(.+)&quot;&gt;.+&lt;/a&gt;&lt;br/&gt;\d+ sec ago&lt;/li&gt;" />
		//    <text            to="http://gamedev.pastebin.com/pastebin.php" post="paste=Send poster={poster} expiry=m format=csharp code2={code}" scrape="&lt;li class=&quot;highlight&quot;&gt;&lt;a href=&quot;(.+)&quot;&gt;.+&lt;/a&gt;&lt;br/&gt;\d+ sec ago&lt;/li&gt;" />
		//    <image
		//        to="http://www.imageshack.us/index.php"
		//        post="MAX_FILE_SIZE=1048576 refer= xml=yes submit=host+it! fileupload={image}"
		//        scrape="&lt;image_link&gt;(.+?)&lt;/image_link&gt;"
		//    />
		//</paste>

		class WebClient : System.Net.WebClient {
			public Uri ResponseUri { get; private set; }

			protected override WebResponse GetWebResponse( WebRequest request ) {
				var response = base.GetWebResponse(request);
				ResponseUri = response.ResponseUri;
				return response;
			}

			protected override WebResponse GetWebResponse( WebRequest request, IAsyncResult result ) {
				var response = base.GetWebResponse(request, result);
				ResponseUri = response.ResponseUri;
				return response;
			}
		}

		static Dictionary< string, HashSet<string> > LanguageKeywords = new Dictionary<string,HashSet<string>>()
			{ { "C++", new HashSet<string>() { "#include", "#define", "using namespace", "namespace std", "namespace boost", "::", "->", "public:", "private:", "protected:" } }
			, { "C#", new HashSet<string>() { "unsafe", "System.", "this.", "throw new", "yield return" } }
			, { "XML", new HashSet<string>() { "<?xml" } } //, "/>", "&lt;", "&amp;", "&gt;", "<!--", "-->" } }
			};

		class Paster {
			public PasteProvider Provider;

			public Dictionary< String, Action<String> > TextPasteLanguages = new Dictionary<String,Action<String>>();
			public Action<Image> ImagePaste = null;
		}

		class UploadState {
			public WebClient Client;
			public Regex Scrape;
		}

		void PostTextTo( string postformat, string to, string code, Regex scrape ) {
			var request
				= String.Join("&",postformat.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries))
				.Replace("{poster}",HttpUtility.UrlEncode(View.Nickname))
				.Replace("{code}"  ,HttpUtility.UrlEncode(code))
				;
			var client = new WebClient() { Encoding = Encoding.UTF8 };
			client.Headers.Add( "Content-Type", "application/x-www-form-urlencoded" );
			client.UploadDataCompleted += PostComplete;
			client.UploadDataAsync( new Uri(to), null, Encoding.UTF8.GetBytes(request), new UploadState() { Client = client, Scrape = scrape } );
		}
		void Write( MemoryStream stream, byte[] data ) { stream.Write(data,0,data.Length); }
		void Write( MemoryStream stream, Encoding encoding, string data ) { Write(stream,encoding.GetBytes(data)); }
		void PostImageTo( string postformat, string to, Image image, Regex scrape ) {
			postformat
				= postformat
				.Replace("{poster}",View.Nickname)
				;
			var encoding = Encoding.UTF8;

			var client = new WebClient() { Encoding = encoding };
			var boundary = "MAULINGMONKEY_DEMANDS_THIS_BOUNDRY_IS_NOT_TO_BE_FOUND_IN_YOUR_IMAGES";
			client.Headers.Add( "Content-Type", "multipart/form-data; boundary="+boundary );
			boundary = "--"+boundary;
			//http://stackoverflow.com/questions/219827/multipart-forms-from-c-client
			//http://www.w3.org/TR/html401/interact/forms.html

			using ( var data = new MemoryStream() ) {
				foreach ( var item in postformat.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries) ) {
					int eq = item.IndexOf("=");
					var name = item.Substring(0,eq);
					var value = item.Substring(eq+1);

					Write(data,encoding,boundary);
					if ( value == "{image}" ) {
						Write(data,encoding
							, "\r\n"
							+ "Content-Disposition: form-data; name=\""+name+"\"; filename=\"clipboard.png\"\r\n"
							+ "Content-Type: image/png\r\n"
							+ "Content-Transfer-Encoding: binary\r\n"
							+ "\r\n"
							);
						using ( var imagedata = new MemoryStream() ) {
							image.Save(imagedata,ImageFormat.Png);
							Write(data,imagedata.ToArray());
						}
						Write(data,encoding,"\r\n");
					} else {
						Write(data,encoding
							, "\r\n"
							+ "Content-Disposition: form-data; name=\""+name+"\"\r\n"
							+ "\r\n"
							+ value+"\r\n"
							);
					}
				}
				Write(data,encoding,boundary);
				Write(data,encoding,"--\r\n");

				string debug = encoding.GetString(data.ToArray());

				client.UploadDataCompleted += PostComplete;
				client.UploadDataAsync( new Uri(to), null, data.ToArray(), new UploadState() { Client = client, Scrape = scrape } );
			}
		}
		void PostComplete(object sender, UploadDataCompletedEventArgs e) {
			var upload = (UploadState)e.UserState;
			if ( upload.Scrape != null ) {
				var result = upload.Client.Encoding.GetString(e.Result);
				Match m = upload.Scrape.Match(result);
				bool success = m.Success;
				if ( success ) View.BeginTryPasteLink(m.Groups[1].Value);
			} else {
				View.BeginTryPasteLink( upload.Client.ResponseUri.ToString() );
			}
			upload.Client.Dispose();
		}
		
		void Post( Paster paster ) {
			var poster = View.Nickname;
			var code  = Clipboard.GetText();
			var image = Clipboard.GetImage();

			if ( image != null ) {
				if ( paster.ImagePaste != null ) paster.ImagePaste(image);
			} else if ( code != "" ) {
				string language = "C#";
				foreach ( var lk in LanguageKeywords ) {
					foreach ( var keyword in lk.Value )
					if ( code.Contains(keyword) )
					{
						language = lk.Key;
						break;
					}
				}

				if ( !paster.TextPasteLanguages.ContainsKey(language) ) language = "default";
				paster.TextPasteLanguages[language](code);
			}
		}

		Action CreateActionFor( XmlNode paste ) {
			var paster = new Paster() { Provider = this };

			foreach ( XmlNode text in paste.SelectNodes("text") ) {
				string lang   = "default";
				string to     = null;
				string post   = null;
				string scrape = null;

				foreach ( XmlAttribute attribute in text.Attributes )
				switch ( attribute.Name )
				{
				case "lang"  : lang   = attribute.Value; break;
				case "to"    : to     = attribute.Value; break;
				case "post"  : post   = attribute.Value; break;
				case "scrape": scrape = attribute.Value; break;

				default:
					throw new FormatException( "Unexpected attribute "+attribute.Name+" in <text> tag" );
				}

				if ( to   == null ) throw new FormatException( "Expected an attribute, to, in <text> tag" );
				if ( post == null ) throw new FormatException( "Expected an attribute, post, in <text> tag" );
				
				paster.TextPasteLanguages.Add( lang, new Action<String>( (code) => PostTextTo(post,to,code,scrape==null?null:new Regex(scrape)) ) );
			}

			foreach ( XmlNode image in paste.SelectNodes("image") ) {
				string to     = null;
				string post   = null;
				string scrape = null;

				foreach ( XmlAttribute attribute in image.Attributes )
				switch ( attribute.Name )
				{
				case "to"    : to     = attribute.Value; break;
				case "post"  : post   = attribute.Value; break;
				case "scrape": scrape = attribute.Value; break;

				default:
					throw new FormatException( "Unexpected attribute "+attribute.Name+" in <image> tag" );
				}

				if ( to   == null ) throw new FormatException( "Expected an attribute, to, in <image> tag" );
				if ( post == null ) throw new FormatException( "Expected an attribute, post, in <image> tag" );

				paster.ImagePaste = new Action<Image>( (img) => PostImageTo(post,to,img,scrape==null?null:new Regex(scrape)) );
			}

			return new Action( () => Post(paster) );
		}

		public override IEnumerable< KeyValuePair<Keys,Action> > Shortcuts { get {
			foreach ( XmlNode paste in Settings.XML.SelectNodes("//paste") ) {
				string shortcut = null;

				foreach ( XmlAttribute attribute in paste.Attributes )
				switch ( attribute.Name )
				{
				case "shortcut": shortcut = attribute.Value; break;

				default:
					throw new FormatException( "Unexpected attribute "+attribute.Name+" in <paste> tag" );
				}

				yield return new KeyValuePair<Keys,Action>( Settings.ReadKeys(shortcut), CreateActionFor(paste) );
			}
		} }
	}
}
