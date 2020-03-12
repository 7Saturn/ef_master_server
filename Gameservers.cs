using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

public class Masterserver {
	public const string VersionString = "0.1";
	private static bool debug = false;
	private static bool verbose = false;
	private static ushort master_port = 27953;
	private static string OwnFileName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

	public static bool GetDebug() {
		return debug;
	}

	public static bool GetVerbose() {
		return verbose;
	}

	public static ushort GetPort() {
		return master_port;
	}

	public static string GetOwnFileName() {
		return OwnFileName;
	}

	public static string GetVersionString() {
		return VersionString;
	}

	private static void ParseArgs(string[] args) {
		if (args.Contains("--help")) {
			ShowHelp();
			Environment.Exit(0);
		}
		if (args.Contains("--debug")) {
			Console.WriteLine("--debug: OK, you want it all...");
			debug = true;
		}
		if (args.Contains("--verbose")) {
			Console.WriteLine("--verbose: I'll be a little less quiet...");
			verbose = true;
		}
		if (args.Contains("--port")) {
            DebugMessage("--port switch found");
			int portswitchposition = Array.IndexOf(args, "--port");
			if (portswitchposition == (args.Length - 1)) {
				Console.WriteLine("--port switch requires a port value for the UDP port to be used for listening.");
				Environment.Exit(2);
			}
			int port;
			if (!Int32.TryParse(args[portswitchposition+1], out port)) {
				Console.WriteLine("The provided --port value '"+args[portswitchposition]+"' cannot be recognized. Missing value?");
				Environment.Exit(2);
			}
			if (port > 65535 || port < 0) {
				Console.WriteLine("The provided --port value must be greater than 0 and less than 65536.");
				Environment.Exit(2);
			}
			if (GetVerbose()) {Console.WriteLine("--port: Using port {0} for incoming connections.", port);}
			master_port = (ushort)port;
		}
	}

	public static void ShowHelp() {
		Console.WriteLine("EF Masterserver Version {0}", GetVersionString());
		Console.WriteLine();
		Console.WriteLine("Usage:");
		Console.WriteLine();
		Console.WriteLine("{0} [--port <portnumber>] [--verbose] [--debug]", OwnFileName);
		Console.WriteLine();
		Console.WriteLine("Switches:");
		Console.WriteLine("--port <portnumber>: Sets the listening port on the value provided, default ist 27953.");
		Console.WriteLine("--verbose:           Shows a little more information on what is currently going on.");
		Console.WriteLine("--debug:             Shows debug messages on what is currently going on.");
		Console.WriteLine();
		Console.WriteLine("{0} --help: Prints this help.", OwnFileName);
	}

	public static int Main(string[] args) {
        ParseArgs(args);
        ServerList.AddServerListFromMaster("master.stvef.org", 27953);
        ServerList.AddServerListFromMaster("efmaster.tjps.eu", 27953);
        ServerList.AddServerListFromMaster("master.stef1.daggolin.de", 27953);
        ServerList.AddServerListFromMaster("192.168.0.2", 27953);
        List<ServerEntry> current_servers = ServerList.get_list();

        foreach (ServerEntry current_entry in current_servers) {
            if (!current_entry.ToString().Equals("")) {
                current_entry.QueryDetails();
                Dictionary <string,string> liste = current_entry.GetData();
                string hostname = "";
                string version = "";
                liste.TryGetValue("hostname", out hostname);
                liste.TryGetValue("version", out version);
                System.Console.WriteLine("{0}: {1} ({2})", version, hostname, current_entry.GetIpRepresentation());
            }
        }
        return 0;
	}

    public static void DebugMessage (string debugmessage) {
        if (GetDebug()) {Console.WriteLine(" debug: {0}", debugmessage);}
    }
}
