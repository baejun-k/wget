using System;
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
			int timeout = 4 * 60 * 1000,
			TextWriter outStream = null, TextWriter errStream = null)
		{
			Trace.Assert(uri != null);

			ResultCode resCode = ResultCode.UNKNOWN;
			credential = credential ?? CredentialCache.DefaultNetworkCredentials;

			var web = new WebClientWithTimeOut(timeout);
			web.Credentials = credential;

			web.DownloadProgressChanged += (s, e) => {
				if (e == null) { return; }
				errStream?.WriteLine("downloading: " + e.BytesReceived + " " + e.TotalBytesToReceive);
			};
			web.DownloadStringCompleted += (s, e) => {
				if (e.Cancelled) {
					resCode = ResultCode.CANCELLED;
					errStream?.WriteLine("cancelled");
				}
				else if (e.Error != null) {
					resCode = ResultCode.ERROR;
					errStream?.WriteLine(e.Error.Message);
					errStream?.WriteLine(e.Error.StackTrace);
				}
				else {
					resCode = ResultCode.COMPLETED;
					outStream?.WriteLine(e.Result);
				}
			};
			try { web.DownloadStringTaskAsync(uri).Wait(); }
			catch (Exception exc) {
				resCode = ResultCode.ERROR;
				errStream?.WriteLine(exc.StackTrace);
				PrintExceptions(exc, errStream, 0);
			}

			web.Dispose();

			return resCode;
		}

		public static ResultCode GetFile(Uri uri, string dstFileName,
			NetworkCredential credential = null, int timeout = 4 * 60 * 1000,
			TextWriter outStream = null, TextWriter errStream = null)
		{
			Trace.Assert(uri != null);

			ResultCode resCode = ResultCode.UNKNOWN;
			credential = credential ?? CredentialCache.DefaultNetworkCredentials;

			string tmpFileName;
			do {
				tmpFileName = Path.Combine(
					Path.GetDirectoryName(dstFileName ?? "./"),
					Path.GetRandomFileName() + ".wgettmp");
			} while (File.Exists(tmpFileName));

			var web = new WebClientWithTimeOut(timeout);
			web.Credentials = credential;

			web.DownloadProgressChanged += (s, e) => {
				if (e == null) { return; }
				errStream?.WriteLine("downloading: " + e.BytesReceived + " " + e.TotalBytesToReceive);
			};
			web.DownloadFileCompleted += (s, e) => {
				if (e.Cancelled) {
					resCode = ResultCode.CANCELLED;
					errStream?.WriteLine("cancelled");
				}
				else if (e.Error != null) {
					resCode = ResultCode.ERROR;
					errStream?.WriteLine(e.Error.Message);
					errStream?.WriteLine(e.Error.StackTrace);
				}
				else {
					resCode = ResultCode.COMPLETED;
				}
			};

			try { web.DownloadFileTaskAsync(uri, tmpFileName).Wait(); }
			catch (Exception exc) {
				resCode = ResultCode.ERROR;
				errStream?.WriteLine(exc.StackTrace);
				PrintExceptions(exc, errStream, 0);
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
				}

				try {
					if (File.Exists(dstFileName)) { File.Delete(dstFileName); }
					File.Move(tmpFileName, dstFileName);
				}
				catch (Exception exc) {
					resCode = ResultCode.ERROR;
					errStream?.WriteLine(exc.StackTrace);
					PrintExceptions(exc, errStream, 0);
				}
			}

			if (File.Exists(tmpFileName)) {
				try { File.Delete(tmpFileName); }
				catch (Exception) { }
			}

			outStream?.WriteLine(resCode.ToString() + " " + dstFileName);

			web.Dispose();

			return resCode;
		}

		private static void PrintExceptions(Exception exc, TextWriter errStream, int level, int depth = -1)
		{
			errStream = errStream ?? Console.Error;
			errStream.WriteLine(exc.Message);
			if (exc.InnerException != null && (depth == -1 || level < depth)) {
				PrintExceptions(exc.InnerException, errStream, level + 1, depth);
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