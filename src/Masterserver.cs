using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

/* requires mono-runtime and libmono-system-core4.0-cil packages under Ubuntu
   resp. Debian.
   requires mono-core package under Suse LEAP and MacOS.
   requires mono-mcs and libmono-cil-dev for compiling under Debian/Ubuntu
   requires libgdiplus for running on FreeBSD */

public class Masterserver {
    public const string VersionString = "0.5.0";
    private static bool useGui = false;
    private static Thread queryOtherMasterServersThread = null;
    private static ushort masterPortV4 = 27953;
    private static ushort masterPortV6 = 27953;
    private static string OwnFileName =
        Environment.GetCommandLineArgs()[0].Replace(
            Directory.GetCurrentDirectory(), ".");
    private static string[] masterServerArray = null;
    private static int interval = 0;
    private static bool v6mode = false;
    private static IPAddress interfaceAddressV4 = null;
    private static IPAddress interfaceAddressV6 = null;

    public static String consoleHelpText;

    public static string getStartCommand() {
        string currentSystemType =
            System.Environment.OSVersion.Platform.ToString();
        if (currentSystemType.Equals("Unix")) {
            return "mono " + OwnFileName;
        }
        else if (currentSystemType.Equals("Win32NT")) {
            return OwnFileName;
        }
        else {
            Console.WriteLine("System: '{0}'", currentSystemType);
            return OwnFileName;
        }
    }

    public static Thread GetOtherMasterServerQueryThread() {
        return queryOtherMasterServersThread;
    }

    public static ushort GetPortV4() {
        return Masterserver.masterPortV4;
    }

    public static ushort GetPortV6() {
        return Masterserver.masterPortV6;
    }

    public static bool InV6Mode() {
        return Masterserver.v6mode;
    }

    public static string GetOwnFileName() {
        return OwnFileName;
    }

    public static string GetVersionString() {
        return VersionString;
    }

    private static void FaultyParameterNotification(string parameter) {
        Console.Error.WriteLine("Parameter '{0}' is unknown.", parameter);
    }

    public static string[] GetMasterServerSources() {
        return masterServerArray;
    }

    public static int GetMasterServerQueryInterval() {
        return interval;
    }

    public static string GetMasterServerListeningInterfaceV4() {
        if (interfaceAddressV4 == null) {
            return "0.0.0.0 (all)";
        }
        else {
            return interfaceAddressV4.ToString();
        }
    }

    public static string GetMasterServerListeningInterfaceV6() {
        if (interfaceAddressV6 == null) {
            return "[::] (all)";
        }
        else {
            return "[" + interfaceAddressV6.ToString() + "]";
        }
    }

    private static void ParseArgs(string[] args) {
        Printer.DebugMessage("Parsing CLI parameters...");
        string[] twoPartParameters = {
            "--copy-from",
            "--interval",
            "--interfacev4",
            "--interfacev6",
            "--port",
            "--portv6"
        };
        string[] onePartParameters = {
            "--debug",
            "--help",
            "--v6mode",
            "--verbose",
            "--withgui"
        };
        if (args.Length == 0) {
            return;
        }
        if (args.Contains("--help")) {
            Printer.DebugMessage("--help found");
            ShowHelp();
            Environment.Exit(0);
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
        Printer.VerboseMessage(  "Starting EF master server ver. "
                               + Masterserver.VersionString + "...");
        if (args.Contains("--withgui")) {
            useGui = true;
        }
        if (args.Contains("--v6mode")) {
            v6mode = true;
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
                && !(twoPartParameters.Contains(
                         args[(Array.IndexOf(args,parameter)) - 1]))) {
                Printer.DebugMessage("Parameter '" + parameter
                                     + "' is unknown.");
                Masterserver.FaultyParameterNotification(parameter);
                ShowHelp();
                Environment.Exit(2);
            }
        }
        if (args.Contains("--port")) {
            Printer.DebugMessage("--port switch found");
            int portswitchposition = Array.IndexOf(args, "--port");
            if (portswitchposition == (args.Length - 1)) {
                Console.Error.WriteLine("--port switch requires a port value for"
                                        + " the UDP port to be used for listening.");
                Environment.Exit(2);
            }
            int port;
            if (!Int32.TryParse(args[portswitchposition + 1], out port)) {
                Console.Error.WriteLine("The provided --port value '"
                                        + args[portswitchposition]
                                        + "' cannot be recognized. Missing"
                                        + " value?");
                Environment.Exit(2);
            }
            if (port > 65535 || port < 0) {
                Console.Error.WriteLine("The provided --port value must be"
                                        + " greater than 0 and less than"
                                        + " 65536.");
                Environment.Exit(2);
            }
            Printer.VerboseMessage("--port: Using port "
                                   + port + " for incoming connections.");
            masterPortV4 = (ushort)port;
        }
        if (args.Contains("--portv6")) {
            Printer.DebugMessage("--portv6 switch found");
            if (!InV6Mode()) {
                Console.Error.WriteLine("--portv6 switch requires to also use"
                                        + " --v6mode. --v6mode was not found.");
                Environment.Exit(2);
            }
            int portswitchposition = Array.IndexOf(args, "--portv6");
            if (portswitchposition == (args.Length - 1)) {
                Console.Error.WriteLine("--portv6 parameter requires a port"
                                        + " value for the UDP port to be used"
                                        + " for listening.");
                Environment.Exit(2);
            }
            int port;
            if (!Int32.TryParse(args[portswitchposition + 1], out port)) {
                Console.Error.WriteLine("The provided --portv6 value '"
                                        + args[portswitchposition]
                                        + "' cannot be recognized. Missing"
                                        + " value?");
                Environment.Exit(2);
            }
            if (port > 65535 || port < 0) {
                Console.Error.WriteLine("The provided --port value must be"
                                        + " greater than 0 and less than"
                                        + " 65536.");
                Environment.Exit(2);
            }
            Printer.VerboseMessage("--port: Using port " + port
                                   + " for incoming connections.");
            masterPortV6 = (ushort)port;
        }
        else {
            if (InV6Mode()) {
                Printer.VerboseMessage("--portv6 was not used, setting it to "
                                       + masterPortV4 + " as well.");
            }
            masterPortV6 = masterPortV4;
        }
        if (args.Contains("--interfacev4")) {
            Printer.DebugMessage("--interfacev4 switch found");
            int interfaceswitchposition = Array.IndexOf(args, "--interfacev4");
            if (interfaceswitchposition == (args.Length - 1)) {
                Console.Error.WriteLine("--interfacev4 switch requires a"
                                        + " network address of the network"
                                        + " interface to be used for"
                                        + " listening.");
                Environment.Exit(2);
            }
            string interfaceString = args[Array.IndexOf(args,
                                                        "--interfacev4") + 1];
            try {
                interfaceAddressV4 = IPAddress.Parse(interfaceString);
                Printer.VerboseMessage("--interfacev4: Using interface "
                                       + interfaceString
                                       + " for incoming connections.");
            }
            catch(FormatException) {
                Console.Error.WriteLine("The provided --interfacev4 value must"
                                        + " be a valid IPv4 address.");
                Environment.Exit(2);
            }
            catch(Exception e) {
                Console.WriteLine("Something unexpected happened:");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
        }
        if (args.Contains("--interfacev6")) {
            Printer.DebugMessage("--interfacev6 switch found");
            if (!InV6Mode()) {
                Console.Error.WriteLine("--interfacev6 switch requires to also"
                                        + " use --v6mode. --v6mode was not"
                                        + " found.");
                Environment.Exit(2);
            }
            int interfaceswitchposition = Array.IndexOf(args, "--interfacev6");
            if (interfaceswitchposition == (args.Length - 1)) {
                Console.Error.WriteLine("--interfacev6 switch requires a"
                                        + " network address of the network"
                                        + " interface to be used for"
                                        + " listening.");
                Environment.Exit(2);
            }
            string interfaceString = args[Array.IndexOf(args, "--interfacev6") + 1];
            try {
                interfaceAddressV6 = IPAddress.Parse(interfaceString);
                Printer.VerboseMessage("--interfacev6: Using interface "
                                       + interfaceString
                                       + " for incoming connections.");
            }
            catch(FormatException) {
                Console.Error.WriteLine("The provided --interfacev6 value must"
                                        + " be a valid IPv6 address.");
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
            Console.Error.WriteLine("--interval switch requires --copy-from"
                                    + " switch.");
            Environment.Exit(2);
        }
        if (args.Contains("--copy-from")) {
            Printer.DebugMessage("--copy-from switch found");
            int masterListPosition = Array.IndexOf(args, "--copy-from");
            if (masterListPosition == (args.Length - 1)) {
                Console.Error.WriteLine("--copy-from switch requires a comma"
                                        + " separated list of servers or IPs,"
                                        + " that should be used for querying of"
                                        + " other master servers.");
                Environment.Exit(2);
            }
            string masterServerString = args[Array.IndexOf(args,
                                                           "--copy-from") + 1];
            if (   onePartParameters.Contains(masterServerString)
                || twoPartParameters.Contains(masterServerString)) {
                Console.Error.WriteLine("--copy-from switch contains no valid"
                                        + " master server hosts.");
                Environment.Exit(2);
            }
            int intervalPosition = -1;

            if (args.Contains("--interval")) {
               Printer.DebugMessage("--interval switch found");
               intervalPosition = Array.IndexOf(args, "--interval");
               if (intervalPosition == (args.Length - 1)) {
                   Console.Error.WriteLine("--interval switch requires an "
                                           + "integer value representing the"
                                           + " time interval for querying other"
                                           + " master servers in seconds. May"
                                           + " not be lower than 60.");
                   Environment.Exit(2);
               }
               if (!Int32.TryParse(args[Array.IndexOf(args, "--interval") + 1],
                                   out interval)) {
                   Console.Error.WriteLine("--interval switch was used but the"
                                           + " provided value {0} does not seem"
                                           + " to be a valid integer value.",
                                           args[Array.IndexOf(
                                                   args,
                                                   "--interval") + 1]);
                   Environment.Exit(2);
               }
               if (interval < 60) {
                   Console.Error.WriteLine("--interval switch was used but the"
                                           + " provided value {0} is not bigger"
                                           + " than 59.",
                                           args[Array.IndexOf(
                                                   args,
                                                   "--interval") + 1]);
                   Environment.Exit(2);
               }
            }
            Printer.DebugMessage("Masterservers: " + masterServerString);
            string commaPattern = "\\s*,\\s*";
            masterServerArray = Regex.Split(masterServerString, commaPattern);
        }
    }

    public static void InitialMasterServerQuery() {
        if (masterServerArray == null ) {
            Printer.DebugMessage("Query foreign master servers feature not in"
                                 + " use, querying nothing.");
            return;
        }
        if (masterServerArray.Length != 0) {
            Printer.DebugMessage("Found following master servers provided:");
            foreach (string server in masterServerArray) {
                Printer.DebugMessage("\"" + server + "\"");
            }

            if (interval == 0) {
                Printer.DebugMessage("No interval given, starting master query"
                                     + " once.");
                ServerList.QueryOtherMasters(masterServerArray);
            }
            else {
                Printer.DebugMessage("Interval " + interval
                                     + " given, starting master query"
                                     + " repeatedly.");
                queryOtherMasterServersThread =
                    ServerList.QueryOtherMastersThreaded(masterServerArray,
                                                         interval);
            }
        }
        else {
            Printer.DebugMessage("Found no master servers provided!");
        }
    }

    public static void ShowHelp() {
        Console.WriteLine(consoleHelpText);
    }

    private static void SetConsoleHelpText() {
        consoleHelpText = "EF Masterserver Version "
                          + Masterserver.GetVersionString();
        consoleHelpText += @"
Copyright (C) 2025  Martin Wohlauer

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.

Usage:

" + getStartCommand() + @" [--v6mode] [--port <portnumber>] [--portv6 <portnumber>] [--interfacev4 <local IPv4 address>] [--interfacev6 <local IPv6 address>] [--copy-from <serverlist> [--interval <number>] [--withgui] [--verbose] [--debug]

Options:
--v6mode
By default the Master Server only works on IPv4 networks. This switch activates the IPv6 capabilities as well.

--port <portnumber>
Sets the listening UDPv4 port to the value provided, default is 27953. Not recommended for standard EF servers, as they cannot connect to another port than the standard port. Only ioQuake3 derivatives can do so.

--portv6 <portnumber>
Sets the listening UPDv6 port to the value provided, default is 27953. If this parameter is not provided, the UDPv6 port is set to the same value, as the UDPv4 port. This parameter can only be used, if --v6mode is also used.

--interfacev4 <local IPv4 address>
Binds the master server to a specific network interface. Requires an IPv4 address of the local network interface to be used.

--interfacev6 <local IPv6 address>
Binds the master server to a specific network interface. Requires an IPv6 address of the local network interface to be used. This parameter can only be used, if --v6mode is also used.

--copy-from <list>
Queries other master servers for their data. Requires a comma separated list of master server names or IPs.

--interval <number>
Defines, how long the time interval between master server queries to other servers is in seconds. May not be less than 60 (= 1 minute). Requires switch --copy-from. Default is off (no repeated querying).

--withgui
Shows the currently known servers in a graphical window.

--verbose
Shows a little more information on what is currently going on.

--debug
Shows debug messages on what is currently going on. Sets --verbose switch active, too.

--help
Prints this help and exits.";
    }

    [STAThread] // Otherwise Copying from the gui server list won't work!
    public static int Main(string[] args) {
        Printer.VerboseMessage("Trying to start up EF master server, version "
                               + GetVersionString() + "...");
        SetConsoleHelpText();
        ServerList.InitializeList(); // Making sure it exists early on.
        QueryStrings.CreateMapping();
        ParseArgs(args);
        if (useGui) {
            Printer.DebugMessage("Trying to start listener thread...");
            HeartbeatListener.StartListenerThreads(interfaceAddressV4,
                                                   interfaceAddressV6);
            /* It does not help to do that in the GUI constructor,
               the window will not appear earlier... */
            InitialMasterServerQuery();
            try {
                /* This will block the stopping of the listener threads until
                   the gui has been stopped via user input. */
                Application.EnableVisualStyles();
                Application.Run (new Gui(VersionString));
            }
            catch (TypeInitializationException) {
                Console.WriteLine("I got a 'TypeInitializationException' here."
                                  + " Usually that means you tried to use the"
                                  + " GUI feature without actually having an"
                                  + " X-server started. I cannot do that.");
                Environment.Exit(1);
            }
            HeartbeatListener.StopListenerThreads();
        }
        else {
            Printer.DebugMessage("Trying to start Listener...");
            /* This will not block, but the entire application will wait until
               all threads are ended (which they never will on their own).
               We could make this interactive here with some sort of console,
               that allows for manually stopping without CTRL + C-ing. */
            HeartbeatListener.StartListenerThreads(interfaceAddressV4,
                                                   interfaceAddressV6);
            InitialMasterServerQuery();
        }
        return 0;
    }

}
