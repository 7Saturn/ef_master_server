using System;
using System.Net.Sockets;
using System.Linq;
using System.Collections.Generic;
using System.IO; 
using System.Text.RegularExpressions;

//requires mono-runtime and libmono-system-core4.0-cil packages under Ubuntu 14 resp. Debian 9
//requires mono-core package under Suse LEAP
//requires mono-mcs for compiling under Debian/Ubuntu

public class Masterserver {
	public const string VersionString = "0.1";
	private static bool debug = false;
	private static bool verbose = false;
	private static ushort master_port = 27953;
	private static string OwnFileName = Environment.GetCommandLineArgs()[0].Replace(Directory.GetCurrentDirectory(), ".");
    
    public static string getStartCommand() {
        string currentSystemType = System.Environment.OSVersion.Platform.ToString();
        if (currentSystemType.Equals("Unix")) {
            return "mono " + OwnFileName;
        } else if (currentSystemType.Equals("Win32NT")) {
            return OwnFileName;
        } else {
            Console.WriteLine("System: '{0}'", currentSystemType);
            return OwnFileName;
        }
    }
    
	public static bool GetDebug() {
		return debug;
	}
	
	public static bool GetVerbose() {
		return verbose;
	}

	public static ushort GetPort() {
		return Masterserver.master_port;
	}

	public static string GetOwnFileName() {
		return OwnFileName;
	}

	public static string GetVersionString() {
		return VersionString;
	}

	private static void FaultyParameterNotification(string parameter) {
        Console.WriteLine("Parameter '{0}' is unknown.", parameter);
    }
    
    
    private static void ParseArgs(string[] args) {
        string[] twoPartParameters = {"--port", "--copy-from"};
        string[] onePartParameters = {"--help", "--debug", "--verbose"};
        if (args.Length == 0) {
            return;
        }
		if (args.Contains("--help")) {
            Masterserver.DebugMessage("--help found");
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
        if (!(onePartParameters.Contains(args[0]) || twoPartParameters.Contains(args[0]))) {
            Masterserver.DebugMessage("First parameter is unknown.");
            Masterserver.FaultyParameterNotification(args[0]);
            ShowHelp();
            Environment.Exit(2);
        }
        foreach (string parameter in args) {
            if (   !(onePartParameters.Contains(parameter))
                && !(twoPartParameters.Contains(parameter))
                && !(twoPartParameters.Contains(args[(Array.IndexOf(args,parameter))-1])))
            {
                Masterserver.DebugMessage("Parameter '"+ parameter +"' is unknown.");
                Masterserver.FaultyParameterNotification(parameter);
                ShowHelp();
                Environment.Exit(2);
            }
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
			if (Masterserver.GetVerbose()) {Console.WriteLine("--port: Using port {0} for incoming connections.", port);}
			master_port = (ushort)port;
		}
        if (args.Contains("--copy-from")) {
            DebugMessage("--copy-from switch found");
            int masterListPosition = Array.IndexOf(args, "--copy-from");
			if (masterListPosition == (args.Length - 1)) {
				Console.WriteLine("--copy-from switch requires a comma separated list of servers or IPs, that should be used for querying of other master servers.");
				Environment.Exit(2);
			}
            string masterServerString = args[Array.IndexOf(args, "--copy-from")+1];
            Masterserver.DebugMessage("Masterservers: " + masterServerString);
            string commaPattern = "\\s*,\\s*";
            string[] masterServerArray = Regex.Split(masterServerString, commaPattern);
            if (masterServerArray.Length != 0) {
                foreach (string masterServer in masterServerArray) {
                    Masterserver.DebugMessage("Working on master server '" + masterServer + "'...");
                    ushort master_port = 27953;
                    string master_host = null;
                    if (!masterServer.Equals("")) {
                        string[] serverParts = Regex.Split(masterServer, ":");
                        int temp_port = 0;
                        if (serverParts.Length == 1) {
                            master_host = serverParts[0];
                        } else if (serverParts.Length == 2) {
                            master_host = serverParts[0];
                            Int32.TryParse(serverParts[1], out temp_port);
                            master_port = (ushort)temp_port;
                        } else {
                            //IPv6...
                            Match parts = Regex.Match(masterServer, "$(.*):(\\d)^");
                            if (parts.Success) {
                                master_host = parts.Value;
                                parts = parts.NextMatch();
                                Int32.TryParse(parts.Value, out temp_port);
                                master_port = (ushort)temp_port;
                            }
                        }
                        if (master_host != null && master_port != 0) {
                            Console.WriteLine("Querying master server '{0}'", masterServer);
                            ServerList.AddServerListFromMaster(master_host, master_port);
                        }
                    }
                }
                //Environment.Exit(0);
            }

        }
	}
	
	public static void ShowHelp() {
		Console.WriteLine("EF Masterserver Version {0}", Masterserver.GetVersionString());
		Console.WriteLine();
		Console.WriteLine("Usage:");
		Console.WriteLine();
		Console.WriteLine("{0} [--port <portnumber>] [--verbose] [--debug]", getStartCommand());
		Console.WriteLine();
		Console.WriteLine("Switches:");
		Console.WriteLine("--port <portnumber>: Sets the listening port to the value provided, default is");
		Console.WriteLine("                     27953.");
        Console.WriteLine("--copy-from <list>   Queries other master servers for their data. Requires a");
        Console.WriteLine("                     comma separated list of master server names or IPs.");
		Console.WriteLine("--verbose:           Shows a little more information on what is currently going");
		Console.WriteLine("                     on.");
		Console.WriteLine("--debug:             Shows debug messages on what is currently going on.");
		Console.WriteLine();
		Console.WriteLine("{0} --help: Prints this help.", OwnFileName);
	}
	
	public static int Main(string[] args) {
        ParseArgs(args);
        HeartbeatListener.StartListener(GetPort());
		return 0;
	}
	
    public static void DebugMessage (string debugmessage) {
        if (GetDebug()) {Console.WriteLine(" debug: {0}", debugmessage);}
    }
}

