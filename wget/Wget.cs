﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace wget {
	public class Wget {
		public enum ResultCode {
			UNKNOWN = -1,
			COMPLETED = 0,
			CANCELLED = 1,
			ERROR = 2
		}

		private static SecurityProtocolType securityProtocolType;
		public static void InitSecurityProtocol(
			SecurityProtocolType sercurityProtocol =
			SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
			SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12)
		{
			securityProtocolType = ServicePointManager.SecurityProtocol;
			ServicePointManager.SecurityProtocol = sercurityProtocol;
		}

		public static ResultCode GetString(Uri uri, NetworkCredential credential = null,
			int timeout = 4 * 60 * 1000)
		{
			Trace.Assert(uri != null);

			ResultCode resCode = ResultCode.UNKNOWN;
			credential = credential ?? CredentialCache.DefaultNetworkCredentials;

			var web = new WebClientWithTimeOut(timeout);
			web.Credentials = credential;

			web.DownloadProgressChanged += (s, e) => {
				if (e == null) { return; }
				Console.Error.WriteLine("downloading: " + e.BytesReceived + " " + e.TotalBytesToReceive);
				Console.Error.Flush();
			};
			web.DownloadStringCompleted += (s, e) => {
				if (e.Cancelled) {
					resCode = ResultCode.CANCELLED;
					Console.Error.WriteLine("cancelled");
					Console.Error.Flush();
				}
				else if (e.Error != null) {
					resCode = ResultCode.ERROR;
					Console.Error.WriteLine(e.Error.Message);
					Console.Error.WriteLine(e.Error.StackTrace);
					Console.Error.Flush();
				}
				else {
					resCode = ResultCode.COMPLETED;
					Console.Out.WriteLine(e.Result);
					Console.Out.Flush();
				}
			};
			try { web.DownloadStringTaskAsync(uri).Wait(); }
			catch (Exception exc) {
				resCode = ResultCode.ERROR;
				Console.Out.WriteLine(exc.StackTrace);
				Console.Out.Flush();
				PrintExceptions(exc, 0);
			}

			web.Dispose();

			return resCode;
		}

		public static ResultCode GetFile(Uri uri, string prefix, string dstFileName,
			NetworkCredential credential = null, int timeout = 4 * 60 * 1000)
		{
			Trace.Assert(uri != null);

			ResultCode resCode = ResultCode.UNKNOWN;
			credential = credential ?? CredentialCache.DefaultNetworkCredentials;

			string tmpFileName;
			do {
				tmpFileName = Path.Combine(
					prefix, Path.GetRandomFileName() + ".wgettmp");
			} while (File.Exists(tmpFileName));

			var web = new WebClientWithTimeOut(timeout);
			web.Credentials = credential;

			web.DownloadProgressChanged += (s, e) => {
				if (e == null) { return; }
				Console.Error.WriteLine("downloading: " + e.BytesReceived + " " + e.TotalBytesToReceive);
				Console.Error.Flush();
			};
			web.DownloadFileCompleted += (s, e) => {
				if (e.Cancelled) {
					resCode = ResultCode.CANCELLED;
					Console.Error.WriteLine("cancelled");
					Console.Error.Flush();
				}
				else if (e.Error != null) {
					resCode = ResultCode.ERROR;
					Console.Error.WriteLine(e.Error.Message);
					Console.Error.WriteLine(e.Error.StackTrace);
					Console.Error.Flush();
				}
				else {
					resCode = ResultCode.COMPLETED;
				}
			};

			try { web.DownloadFileTaskAsync(uri, tmpFileName).Wait(); }
			catch (Exception exc) {
				resCode = ResultCode.ERROR;
				Console.Error.WriteLine(exc.StackTrace);
				Console.Error.Flush();
				PrintExceptions(exc, 0);
			}

			if (resCode == ResultCode.COMPLETED) {
				if (dstFileName == null) {
					if (web.ResponseHeaders.Get("Content-Disposition") != null) {
						string _tmp = web.ResponseHeaders.Get("Content-Disposition");
						_tmp = _tmp.Substring(_tmp.IndexOf("filename=") + 9).Replace("\"", "");
						dstFileName = _tmp.Split(';')[0];
					}
					else {
						dstFileName = Path.GetFileName(uri.AbsolutePath);
					}
					dstFileName = Path.Combine(prefix, dstFileName);
				}

				try {
					if (File.Exists(dstFileName)) { File.Delete(dstFileName); }
					File.Move(tmpFileName, dstFileName);
				}
				catch (Exception exc) {
					resCode = ResultCode.ERROR;
					Console.Error.WriteLine(exc.StackTrace);
					Console.Error.Flush();
					PrintExceptions(exc, 0);
				}
			}

			if (File.Exists(tmpFileName)) {
				try { File.Delete(tmpFileName); }
				catch (Exception) { }
			}

			Console.Out.WriteLine(resCode.ToString() + " " + dstFileName);
			Console.Out.Flush();

			web.Dispose();

			return resCode;
		}

		private static void PrintExceptions(Exception exc, int level, int depth = -1)
		{
			Console.Out.WriteLine(exc.Message);
			Console.Out.Flush();
			if (exc.InnerException != null && (depth == -1 || level < depth)) {
				PrintExceptions(exc.InnerException, level + 1, depth);
			}
		}

		public static void Release()
		{
			ServicePointManager.SecurityProtocol = securityProtocolType;
		}
	}

	class WebClientWithTimeOut : WebClient {
		public int TimeOutMilliseconds { get; set; }
		public WebClientWithTimeOut(int timeoutMilli = 3 * 60 * 1000) {
			TimeOutMilliseconds = timeoutMilli;
		}

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest w = base.GetWebRequest(address);
			w.Timeout = TimeOutMilliseconds;
			return w;
		}
	}
}