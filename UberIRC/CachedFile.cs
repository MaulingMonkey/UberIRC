using System;
using System.IO;
using System.Net;

namespace UberIRC
{
	abstract class CachedFile
	{
		public readonly string LocalPath;
		public readonly string RemotePath;
		public TimeSpan CacheAtLeast = TimeSpan.FromDays(1), Timeout = TimeSpan.FromMinutes(10);
		DateTime LastTry = DateTime.MinValue;

		public CachedFile( string local, string remote )
		{
			LocalPath = local;
			RemotePath = remote;

			WebClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler( WebClient_DownloadFileCompleted );
			RecoverIfNecessary();
			BeginDownloadIfOutOfDate();
		}

		protected abstract void OnDownloadComplete();

		void WebClient_DownloadFileCompleted( object sender, System.ComponentModel.AsyncCompletedEventArgs e )
		{
			if( !File.Exists(LocalTempPath) )
				return; // some sort of error

			File.Delete(LocalPath);
			File.Move(LocalTempPath,LocalPath);
			OnDownloadComplete();
		}

		void RecoverIfNecessary()
		{
			if( File.Exists(LocalTempPath) && !File.Exists(LocalPath) )
				File.Move(LocalTempPath,LocalPath); // Only rename, download might've been interrupted
			OnDownloadComplete();
		}

		void BeginDownloadIfOutOfDate()
		{
			var now = DateTime.Now;
			var ft = File.GetLastWriteTime(LocalPath);
			if( LastTry+Timeout < now && (!File.Exists(LocalPath) || ft > now || ft+CacheAtLeast < now) )
			{
				WebClient.DownloadFileAsync(new Uri(RemotePath),LocalTempPath);
				LastTry = DateTime.Now;
			}
		}

		private string LocalTempPath { get { return LocalPath+"2"; }}
		private readonly WebClient WebClient = new WebClient();
	}
}
