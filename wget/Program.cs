﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace wget {
	class Program {
		private static readonly string HelpStr =
@"Usage
    wget.exe URL_ADDRESS [options]
	wget.exe file_url -x -O ./newdir/downloadfile

URL_ADDRESS
    The url address to request downloads.

Options
    HTTP:
      -u / --user     : The user of the credential.
      -p / --password : The password of the credential.
      -T / --timeout  : set the read timeout to SECONDS.

    Dowload:
      -O / --output-document    : write documents to FILE.
      -x / --force-directories  : force creation of directories.
      -P / --directory-prefix   : save files to PREFIX/...
      -S / --string             : Output as a string.

    -h / --help : print help.
Result Value
    UNKNOWN = -1,
    COMPLETED = 0,
    CANCELLED = 1,
    ERROR = 2
";
		
		public static void PrintError(string msg) {
			Console.Error.WriteLine(msg);
		}

		static void Main(string[] args) {
			if(args.Length < 1) {
				PrintError(HelpStr);
				Environment.Exit(1);
			}

			bool? printString = null;
			Uri uri = null;
			string user = null;
			string pwd = null;
			string dstFileName = null;
			string prefixDirName = null;
			bool? createDir = null;
			int? timeout = null;

			uri = new Uri(args[0]);
			try {
				for (int i = 1; i < args.Length; i++) {
					switch (args[i]) {
						case "-S":
						case "--string":
							if (printString == null) { printString = true; }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
						case "-u":
						case "--user":
							if (user == null) { user = args[++i]; }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
						case "-p":
						case "--password":
							if (pwd == null) { pwd = args[++i]; }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
						case "-T":
						case "--timeout":
							if (timeout == null) { timeout = int.Parse(args[++i]) * 1000; }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
						case "-O":
						case "--output-document":
							if (dstFileName == null) { dstFileName = args[++i]; }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
						case "-x":
						case "--force-directories":
							if (createDir == null) { createDir = true; }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
						case "-P":
						case "--directory-prefix":
							if (prefixDirName == null) { prefixDirName = args[++i]; }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
						case "-h":
						case "--help":
							PrintError(HelpStr); Environment.Exit(0);
							break;
						default:
							PrintError(HelpStr); Environment.Exit(1);
							break;
					}
				}
			}
			catch (Exception) {
				PrintError(HelpStr);
				Environment.Exit(1);
			}

			NetworkCredential credential = new NetworkCredential()
			{
				UserName = user ?? CredentialCache.DefaultNetworkCredentials.UserName,
				Password = pwd ?? CredentialCache.DefaultNetworkCredentials.Password
			};

			Wget.ResultCode res = Wget.ResultCode.UNKNOWN;
			Wget.InitSecurityProtocol();
			if (printString == true) {
				res = Wget.GetString(uri, credential, timeout ?? (3 * 60 * 1000));
			}
			else {
				if (prefixDirName == null) {
					if (dstFileName == null) { prefixDirName = AppDomain.CurrentDomain.BaseDirectory; }
					else { prefixDirName = Path.GetDirectoryName(dstFileName); }
				}
				else if (dstFileName != null) {
					if (!Path.IsPathRooted(dstFileName)) {
						dstFileName = Path.Combine(prefixDirName, dstFileName);
					}
				}

				if (createDir == true) {
					Directory.CreateDirectory(prefixDirName);
					if (dstFileName != null) {
						Directory.CreateDirectory(Path.GetDirectoryName(dstFileName));
					}
				}
				res = Wget.GetFile(uri, prefixDirName, dstFileName, credential, timeout ?? (3 * 60 * 1000));
			}
			Wget.Release();

			Environment.Exit((int)res);
		}

	}
}
