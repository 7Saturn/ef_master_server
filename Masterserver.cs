using System;
using System.Net.Sockets;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

//requires mono-runtime and libmono-system-core4.0-cil packages under Ubuntu 14 resp. Debian 9
//requires mono-core package under Suse LEAP
//requires mono-mcs and libmono-cil-dev for compiling under Debian/Ubuntu

public class Masterserver {
    public const string VersionString = "0.2";
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
        string[] twoPartParameters = {"--port", "--copy-from", "--interval"};
        string[] onePartParameters = {"--help", "--debug", "--verbose"};
        if (args.Length == 0) {
            return;
        }
        if (args.Contains("--help")) {
            Masterserver.DebugMessage("--help found");
            ShowHelp();
            Environment.Exit(0);
        }
        if (   args.Contains("--debug")
            || args.Contains("--verbose")) {
			Console.WriteLine("Starting EF master server ver. {0}...", Masterserver.VersionString);
        }
        if (args.Contains("--debug")) {
            Console.WriteLine("--debug: OK, you want it all...");
            debug = true;
            verbose = true;
        }
        if (args.Contains("--verbose")) {
            Console.WriteLine("--verbose: I'll be a little less quiet...");
            verbose = true;
        }
        if (!(   onePartParameters.Contains(args[0])
              || twoPartParameters.Contains(args[0]))) {
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
                Masterserver.DebugMessage("Parameter '" + parameter + "' is unknown.");
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
            if (!Int32.TryParse(args[portswitchposition + 1], out port)) {
                Console.WriteLine("The provided --port value '" + args[portswitchposition] + "' cannot be recognized. Missing value?");
                Environment.Exit(2);
            }
            if (port > 65535 || port < 0) {
                Console.WriteLine("The provided --port value must be greater than 0 and less than 65536.");
                Environment.Exit(2);
            }
            if (Masterserver.GetVerbose()) {Console.WriteLine("--port: Using port {0} for incoming connections.", port);}
            master_port = (ushort)port;
        }
        if (!args.Contains("--copy-from") && args.Contains("--interval")) {
            Console.WriteLine("--interval switch requires --copy-from switch.");
            Environment.Exit(2);
        }
        if (args.Contains("--copy-from")) {
            DebugMessage("--copy-from switch found");
            int masterListPosition = Array.IndexOf(args, "--copy-from");
            if (masterListPosition == (args.Length - 1)) {
                Console.WriteLine("--copy-from switch requires a comma separated list of servers or IPs, that should be used for querying of other master servers.");
                Environment.Exit(2);
            }
            string masterServerString = args[Array.IndexOf(args, "--copy-from") + 1];
            if (onePartParameters.Contains(masterServerString) || twoPartParameters.Contains(masterServerString)) {
                Console.WriteLine("--copy-from switch contains no valid master server hosts.");
                Environment.Exit(2);
            }
            int intervalPosition = -1;
            int interval = 0;
            if (args.Contains("--interval")) {
                DebugMessage("--interval switch found");
                intervalPosition = Array.IndexOf(args, "--interval");
                if (intervalPosition == (args.Length - 1)) {
                    Console.WriteLine("--interval switch requires an integer value representing the time interval for querying other master servers in seconds. May not be lower than 60.");
                    Environment.Exit(2);
                }
                bool worked = Int32.TryParse(args[Array.IndexOf(args, "--interval") + 1], out interval);
                if (!worked) {
                    Console.WriteLine("--interval switch was used but the following parameter {0} does not seem to be a valid integer value.", args[Array.IndexOf(args, "--interval") + 1]);
                    Environment.Exit(2);
                }
                if (interval < 60) {
                    Console.WriteLine("--interval switch was used but the following parameter {0} is not bigger than 59.", args[Array.IndexOf(args, "--interval") + 1]);
                    Environment.Exit(2);
                }
            }
            Masterserver.DebugMessage("Masterservers: " + masterServerString);
            string commaPattern = "\\s*,\\s*";
            string[] masterServerArray = Regex.Split(masterServerString, commaPattern);
            if (masterServerArray.Length != 0) {
                DebugMessage("Found following master servers provided:");
                foreach (string server in masterServerArray) {
                    DebugMessage("\"" + server + "\"");
                }

                if (interval == 0) {
                    DebugMessage("No interval given, starting master query once.");
                    ServerList.QueryOtherMasters(masterServerArray);
                } else {
                    DebugMessage("Interval " + interval + " given, starting master query repeatedly.");
                    ServerList.QueryOtherMastersThreaded(masterServerArray, interval);
                }
                //Environment.Exit(0);
            }
            else {
                DebugMessage("Found no master servers provided!");
            }
        }
    }

    public static void ShowHelp() {
        Console.WriteLine("EF Masterserver Version {0}", Masterserver.GetVersionString());
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("{0} [--port <portnumber>] [--copy-from <serverlist> [--interval <number>]] [--verbose] [--debug]", getStartCommand());
        Console.WriteLine();
        Console.WriteLine("Switches:");
        Console.WriteLine("--copy-from <list>   Queries other master servers for their data. Requires a");
        Console.WriteLine("                     comma separated list of master server names or IPs.");
        Console.WriteLine("--interval <number>  Defines, how long the time interval between master server");
        Console.WriteLine("                     queries to other servers is in seconds. May not be less");
        Console.WriteLine("                     than 60 (= 1 minute). Requires switch --copy-from.");
        Console.WriteLine("                     Default is off (no repeated querying).");
        Console.WriteLine("--port <portnumber>: Sets the listening port to the value provided, default is");
        Console.WriteLine("                     27953. Not recommended for standard EF servers, as they");
        Console.WriteLine("                     cannot connect to another port than the standard port.");
        Console.WriteLine("                     Only ioQuake3 derivatives can do so.");
        Console.WriteLine("--verbose:           Shows a little more information on what is currently going");
        Console.WriteLine("                     on.");
        Console.WriteLine("--debug:             Shows debug messages on what is currently going on.");
        Console.WriteLine("                     Sets --verbose switch active, too.");
        Console.WriteLine();
        Console.WriteLine("--help:              Prints this help and exits.");
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
