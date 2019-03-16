using System;
using System.Net;

namespace wget {
	class Program {
		private static readonly string HelpStr =
@"Usage
    wget.exe URL_ADDRESS [function] [options]
	wget.exe file_url --overwrite false

URL_ADDRESS
    The url address to request downloads.

Function
    file   : Download the file. (default)
    string : Download the string.

Options
    Common options of all options:
      -u / --user     : The user of the credential.
      -p / --password : The password of the credential.

    Parameters of ""file"" options:
      -n / --name        : The name of the file to save local.
                           default value is the name provided by url.
      -o / --overwrite   : Overwrite when a file with the same name exists.
                           default value is true.
";
		
		public static void PrintError(string msg) {
			Console.Error.WriteLine(msg);
		}

		static void Main(string[] args) {
			if(args.Length < 1) {
				PrintError(HelpStr);
				Environment.Exit(1);
			}

			string function = null;
			Uri uri = null;
			string user = null;
			string pwd = null;
			string dstFileName = null;
			bool? overwrite = null;

			uri = new Uri(args[0]);
			try {
				for (int i = 1; i < args.Length; i++) {
					switch (args[i].ToLower()) {
						case "string":
							if (function == null) { function = "string"; }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
						case "file":
							if (function == null) { function = "file"; }
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
						case "-o":
						case "--overwrite":
							if (overwrite == null) { overwrite = bool.Parse(args[++i]); }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
						case "-n":
						case "--name":
							if (dstFileName == null) { dstFileName = args[++i]; }
							else { PrintError(HelpStr); Environment.Exit(1); }
							break;
					}
				}
			}
			catch (Exception) {
				PrintError(HelpStr);
				Environment.Exit(1);
			}

			function = function ?? "file";

			NetworkCredential credential = new NetworkCredential()
			{
				UserName = user ?? CredentialCache.DefaultNetworkCredentials.UserName,
				Password = pwd ?? CredentialCache.DefaultNetworkCredentials.Password
			};

			Wget.ResultCode res = Wget.ResultCode.UNKNOWN;
			Wget.InitSecurityProtocol();
			if (function.Equals("file")) {
				res = Wget.GetFile(uri, dstFileName, overwrite ?? true, credential, Console.Out, Console.Error);
			}
			else if (function.Equals("string")) {
				res = Wget.GetString(uri, credential, Console.Out, Console.Error);
			}
			Wget.Release();

			Environment.ExitCode = (int)res;
		}

	}
}
