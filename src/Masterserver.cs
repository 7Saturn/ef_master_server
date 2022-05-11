using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

//requires mono-runtime and libmono-system-core4.0-cil packages under Ubuntu 14 resp. Debian 9
//requires mono-core package under Suse LEAP and MacOS
//requires mono-mcs and libmono-cil-dev for compiling under Debian/Ubuntu
//requires libgdiplus for running on FreeBSD

public class Masterserver {
    public const string VersionString = "0.4.2";
    private static bool useGui = false;
    private static Thread queryOtherMasterServersThread = null;
    private static ushort master_port = 27953;
    private static string OwnFileName = Environment.GetCommandLineArgs()[0].Replace(Directory.GetCurrentDirectory(), ".");
    private static string[] masterServerArray;
    private static int interval = 0;
    private static IPAddress interfaceAddress = null;

    public static String consoleHelpText;

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

    public static Thread GetOtherMasterServerQueryThread() {
        return queryOtherMasterServersThread;
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

    public static string[] GetMasterServerSources() {
        return masterServerArray;
    }

    public static int GetMasterServerQueryInterval() {
        return interval;
    }

    public static string GetMasterServerListeningInterface() {
        if (interfaceAddress == null) {
            return "0.0.0.0 (all)";
        }
        else {
            return interfaceAddress.ToString();
        }
    }


    private static void ParseArgs(string[] args) {
        string[] twoPartParameters = {"--port", "--copy-from", "--interval", "--interface"};
        string[] onePartParameters = {"--help", "--debug", "--verbose", "--withgui"};
        if (args.Length == 0) {
            return;
        }
        if (args.Contains("--help")) {
            Printer.DebugMessage("--help found");
            ShowHelp();
            Environment.Exit(0);
        }
        if (   args.Contains("--debug")
            || args.Contains("--verbose")) {
			Console.WriteLine("Starting EF master server ver. {0}...", Masterserver.VersionString);
        }
        if (args.Contains("--withgui")) {
            useGui = true;
        }
        if (args.Contains("--verbose")) {
            Console.WriteLine("--verbose: I'll be a little less quiet...");
            Printer.SetVerbose(true);
        }
        if (args.Contains("--debug")) {
            Console.WriteLine("--debug: OK, you want it all...");
            Printer.SetDebug(true);
            Printer.SetVerbose(true);
        }
        if (!(   onePartParameters.Contains(args[0])
              || twoPartParameters.Contains(args[0]))) {
            Printer.DebugMessage("First parameter is unknown.");
            Masterserver.FaultyParameterNotification(args[0]);
            ShowHelp();
            Environment.Exit(2);
        }
        foreach (string parameter in args) {
            if (   !(onePartParameters.Contains(parameter))
                && !(twoPartParameters.Contains(parameter))
                && !(twoPartParameters.Contains(args[(Array.IndexOf(args,parameter))-1])))
            {
                Printer.DebugMessage("Parameter '" + parameter + "' is unknown.");
                Masterserver.FaultyParameterNotification(parameter);
                ShowHelp();
                Environment.Exit(2);
            }
        }
        if (args.Contains("--port")) {
           Printer.DebugMessage("--port switch found");
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
            Printer.VerboseMessage("--port: Using port " + port + " for incoming connections.");
            master_port = (ushort)port;
        }
        if (args.Contains("--interface")) {
            Printer.DebugMessage("--interface switch found");
            int interfaceswitchposition = Array.IndexOf(args, "--interface");
            if (interfaceswitchposition == (args.Length - 1)) {
                Console.WriteLine("--interface switch requires a network address of the network interface to be used for listening.");
                Environment.Exit(2);
            }
            string interfaceString = args[Array.IndexOf(args, "--interface") + 1];;
            try {
                interfaceAddress = IPAddress.Parse(interfaceString);
                Printer.VerboseMessage("--interface: Using interface " + interfaceString + " for incoming connections.");
            }
            catch(FormatException) {
                Console.WriteLine("The provided --interface value must be a valid IPv4 address.");
                Environment.Exit(2);
            }
            catch(Exception e) {
                Console.WriteLine("Something unexpected happened:");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
        }
        if (   !args.Contains("--copy-from")
            && args.Contains("--interval")) {
            Console.WriteLine("--interval switch requires --copy-from switch.");
            Environment.Exit(2);
        }
        if (args.Contains("--copy-from")) {
            Printer.DebugMessage("--copy-from switch found");
            int masterListPosition = Array.IndexOf(args, "--copy-from");
            if (masterListPosition == (args.Length - 1)) {
                Console.WriteLine("--copy-from switch requires a comma separated list of servers or IPs, that should be used for querying of other master servers.");
                Environment.Exit(2);
            }
            string masterServerString = args[Array.IndexOf(args, "--copy-from") + 1];
            if (   onePartParameters.Contains(masterServerString)
                || twoPartParameters.Contains(masterServerString)) {
                Console.WriteLine("--copy-from switch contains no valid master server hosts.");
                Environment.Exit(2);
            }
            int intervalPosition = -1;

            if (args.Contains("--interval")) {
               Printer.DebugMessage("--interval switch found");
               intervalPosition = Array.IndexOf(args, "--interval");
               if (intervalPosition == (args.Length - 1)) {
                   Console.WriteLine("--interval switch requires an integer value representing the time interval for querying other master servers in seconds. May not be lower than 60.");
                   Environment.Exit(2);
               }
               if (!Int32.TryParse(args[Array.IndexOf(args, "--interval") + 1],
                                   out interval)) {
                   Console.WriteLine("--interval switch was used but the following parameter {0} does not seem to be a valid integer value.", args[Array.IndexOf(args, "--interval") + 1]);
                   Environment.Exit(2);
               }
               if (interval < 60) {
                   Console.WriteLine("--interval switch was used but the following parameter {0} is not bigger than 59.", args[Array.IndexOf(args, "--interval") + 1]);
                   Environment.Exit(2);
               }
            }
            Printer.DebugMessage("Masterservers: " + masterServerString);
            string commaPattern = "\\s*,\\s*";
            masterServerArray = Regex.Split(masterServerString, commaPattern);
            if (masterServerArray.Length != 0) {
               Printer.DebugMessage("Found following master servers provided:");
               foreach (string server in masterServerArray) {
                   Printer.DebugMessage("\"" + server + "\"");
               }

               if (interval == 0) {
                   Printer.DebugMessage("No interval given, starting master query once.");
                   ServerList.QueryOtherMasters(masterServerArray);
               } else {
                   Printer.DebugMessage("Interval " + interval + " given, starting master query repeatedly.");
                   queryOtherMasterServersThread = ServerList.QueryOtherMastersThreaded(masterServerArray, interval);
               }
               //Environment.Exit(0);
            }
            else {
               Printer.DebugMessage("Found no master servers provided!");
            }
        }
    }

    public static void ShowHelp() {
        Console.WriteLine(consoleHelpText);
    }

    public static int Main(string[] args) {
        consoleHelpText = "EF Masterserver Version " + Masterserver.GetVersionString();
        consoleHelpText += @"
Copyright (C) 2022  Martin Wohlauer

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.

Usage:

" + getStartCommand();
        consoleHelpText += @" [--port <portnumber>] [--interface <local IP address>] [--copy-from <serverlist> [--interval <number>]] [--verbose] [--debug] [--withgui]

Switches:
--copy-from <list>
Queries other master servers for their data. Requires a comma separated list of master server names or IPs.

--interval <number>
Defines, how long the time interval between master server queries to other servers is in seconds. May not be less than 60 (= 1 minute). Requires switch --copy-from. Default is off (no repeated querying).

--port <portnumber>
Sets the listening port to the value provided, default is 27953. Not recommended for standard EF servers, as they cannot connect to another port than the standard port. Only ioQuake3 derivatives can do so.

--interface <local IP address>
Binds the master server to a specific network interface. Requires an IPv4 address of the local network interface to be used.

--withgui
Shows the currently known servers in a graphical window.

--verbose
Shows a little more information on what is currently going on.

--debug
Shows debug messages on what is currently going on. Sets --verbose switch active, too.

--help
Prints this help and exits.";

        ParseArgs(args);
        if (useGui) {
            Printer.DebugMessage("Trying to start listener thread...");
            HeartbeatListener.StartListenerThread(interfaceAddress, GetPort());
            try {
                Application.EnableVisualStyles();
                Application.Run (new Gui(VersionString));
            }
            catch (TypeInitializationException) {
                Console.WriteLine("I got a 'TypeInitializationException' here. Usually that means you tried to use the GUI feature without actually having an X-server started. I cannot do that.");
                Environment.Exit(1);
            }
            HeartbeatListener.StopListenerThread();
        }
        else {
            Printer.DebugMessage("Trying to start Listener...");
            HeartbeatListener.StartListener(interfaceAddress, GetPort());
        }
        return 0;
    }

}
