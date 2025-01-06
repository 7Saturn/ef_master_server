using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;

class HeartbeatListener {
    private static Thread listenerThreadHandleV4;
    private static Thread listenerThreadHandleV6;

    public static void StartListenerThreads(IPAddress interfaceAddressV4,
                                            IPAddress interfaceAddressV6) {
        if (Masterserver.InV6Mode()) {
            if (interfaceAddressV6 == null) {
                HeartbeatListener.StartListenerThread(
                    AddressFamily.InterNetworkV6,
                    Masterserver.GetPortV6());
            }
            else {
                HeartbeatListener.StartListenerThread(interfaceAddressV6,
                                                      Masterserver.GetPortV6());
            }
            if (Printer.GetVerbose()) {
                string readyMessage = "Ready for incoming IPv6 connections";
                if (interfaceAddressV6 != null) {
                    readyMessage += " on interface " + interfaceAddressV6;
                }
                readyMessage += ", UDP port " + Masterserver.GetPortV6() + ".";
                Printer.VerboseMessage(readyMessage);
            }
        }
        if (interfaceAddressV4 == null) {
            HeartbeatListener.StartListenerThread(AddressFamily.InterNetwork,
                                                  Masterserver.GetPortV4());
        }
        else {
            HeartbeatListener.StartListenerThread(interfaceAddressV4,
                                                  Masterserver.GetPortV4());
        }
        if (Printer.GetVerbose()) {
            string readyMessage = "Ready for incoming IPv4 connections";
            if (interfaceAddressV4 != null) {
                readyMessage += " on interface " + interfaceAddressV4;
            }
            readyMessage += ", UDP port " + Masterserver.GetPortV4() + ".";
            Printer.VerboseMessage(readyMessage);
        }
    }

    public static void StopListenerThreads() {
        Printer.VerboseMessage("Shutting down...");
        HeartbeatListener.StopListenerThreadV4();
        if (Masterserver.InV6Mode()) {
            HeartbeatListener.StopListenerThreadV6();
        }
    }

    public static Thread StartListenerThread(IPAddress localAddress,
                                             ushort listenPort = 27953) {
        Printer.DebugMessage("Starting listener thread.");
        if (NetworkBasics.IsIPv6Address(localAddress)) {
            listenerThreadHandleV6 = new Thread(() =>
                                                StartListener(localAddress,
                                                              listenPort));
            listenerThreadHandleV6.Start();
            return listenerThreadHandleV6;
        }
        if (NetworkBasics.IsIPv4Address(localAddress)) {
            listenerThreadHandleV4 = new Thread(() =>
                                                StartListener(localAddress,
                                                              listenPort));
            listenerThreadHandleV4.Start();
            return listenerThreadHandleV4;
        }
        return null;
    }

    public static Thread StartListenerThread(AddressFamily ipProtocol,
                                             ushort listenPort = 27953) {
        Printer.DebugMessage("Starting listener thread.");
        if (NetworkBasics.IsIPv6Address(ipProtocol)) {
            listenerThreadHandleV6 = new Thread(() =>
                                                StartListener(IPAddress.IPv6Any,
                                                              listenPort));
            listenerThreadHandleV6.Start();
            return listenerThreadHandleV6;
        }
        if (NetworkBasics.IsIPv4Address(ipProtocol)) {
            listenerThreadHandleV4 = new Thread(() =>
                                                StartListener(IPAddress.Any,
                                                              listenPort));
            listenerThreadHandleV4.Start();
            return listenerThreadHandleV4;
        }
        return null;
    }

    public static void StopListenerThreadV4() {
        Printer.DebugMessage("Stopping listener thread.");
        listenerThreadHandleV4.Abort();
    }

    public static void StopListenerThreadV6() {
        Printer.DebugMessage("Stopping listener thread.");
        listenerThreadHandleV6.Abort();
    }

    public static void StartListener(IPAddress localAddress,
                                     ushort listenPort = 27953) {
        Printer.DebugMessage("StartListener");
        /* The listenPort value will change, once someone will contact us on the
           initially given port. So we only need it for the moment. Later it
           will tell us the port of the other guy, contacting us! This is also
           why we will need localAddress and listenPort, when we refer to
           ourselves. Otherwise we will forget/misremember.. */
        IPEndPoint serverEndpoint = new IPEndPoint(localAddress, listenPort);
        Printer.DebugMessage("after endpoint.");
        UdpClient listener = NetworkBasics.NewLocalServer(serverEndpoint);
        try {
            Printer.DebugMessage("Entered main listener loop for "
                                 + localAddress.AddressFamily + ".");
            while (true) {
                byte[] receivedbytes = null;
                try {
                    receivedbytes = listener.Receive(ref serverEndpoint);
                }
                catch (SocketException e) {
                    if (e.ErrorCode == 10054) { // WSAECONNRESET
                        Console.Error.WriteLine("Connection to IP {0} and port "
                                                + "{1} lost. No data received.",
                                                localAddress,
                                                listenPort);
                        // Will be treated like irrelevant data, aka discarded:
                        receivedbytes = null;
                    }
                    else {
                        Console.Error.WriteLine("Something unexpected happend,"
                                                + " while trying to open or"
                                                + " read with local IP end"
                                                + " point:\n");
                        Console.Error.WriteLine("Error-Code: " + e.ErrorCode);
                        Console.Error.WriteLine("Sorry, cannot continue the"
                                                + " master server...");
                        Environment.Exit(1);
                    }
                }
                ushort destinationPort = (ushort) serverEndpoint.Port;
                QueryStrings.requestType requestType =
                    QueryStrings.GetRequestType(receivedbytes,
                                                destinationPort,
                                                Masterserver.InV6Mode());
                if (requestType == QueryStrings.requestType.heartbeat) {
                    ProcessHeartbeatRequest(serverEndpoint);
                }
                /* This is not standard issue. Original EF did never know or
                   support this. But it makes querying other masters for the
                   purpose of running a master yourself a lot easier/faster.
                   Note: Dump always dumps *all* servers, regardless of full/
                   empty/wants IPv6 or not. */
                if (requestType == QueryStrings.requestType.dump) {
                    ProcessDumpListRequest(serverEndpoint,
                                           localAddress,
                                           listener);
                }
                if (      requestType == QueryStrings.requestType.listIpV4
                       && NetworkBasics.IsIPv6Address(serverEndpoint)
                    ||    requestType == QueryStrings.requestType.listIpV6
                       && NetworkBasics.IsIPv4Address(serverEndpoint)) {
                    Printer.DebugMessage("IP version does not fit query header."
                                         + " Sending no response.");
                }
                if (requestType == QueryStrings.requestType.listIpV4) {
                    ProcessIpV4ListRequest(serverEndpoint,
                                           receivedbytes,
                                           listener);
                }
                if (   requestType == QueryStrings.requestType.listIpV6
                    && NetworkBasics.IsIPv6Address(serverEndpoint)) {
                    ProcessIpV6ListRequest(serverEndpoint,
                                           receivedbytes,
                                           listener);
                }
                if (requestType == QueryStrings.requestType.none) {
                    if (receivedbytes != null) {
                        Printer.DebugMessage("I got that stuff here. Do you"
                                             + " recognize any of this?!?");
                        Printer.DebugMessage(
                            Encoding.ASCII.GetString(receivedbytes));
                    }
                    /* Else case is likely a problem with the socket (closed)
                       which we will not report in detail here. It was already
                       reported above, as exception. */
                }
            }
        }
        catch (SocketException e) {
            Console.Error.WriteLine(e);
            Console.Error.WriteLine("Sorry, cannot start the master server...");
            Environment.Exit(1);
        }
        finally {
            listener.Close();
        }
    }

    private static void ProcessHeartbeatRequest(IPEndPoint newServerEndpoint) {
        Printer.VerboseMessage("---- Received heartbeat from "
                               + newServerEndpoint + " ----");
        ServerEntry newOne = new ServerEntry(newServerEndpoint.Address,
                                             (ushort)newServerEndpoint.Port);
        ServerList.AddServer(newOne);
        Printer.DebugMessage("New ones protocol: " + newOne.GetProtocol());
    }

    private static void ProcessDumpListRequest(IPEndPoint serverEndpoint,
                                               IPAddress  localAddress,
                                               UdpClient  listener) {
        Printer.DebugMessage("---- Received server dump query request from "
                             + serverEndpoint + " ----");
        byte[] getserversResponse = null;
        if (NetworkBasics.IsIPv6Address(serverEndpoint)) {
            Printer.DebugMessage("Using server_list_response_head_v6");
            getserversResponse =
                QueryStrings.GetByteArray(
                    QueryStrings.stringType.server_list_response_head_v6);
        }
        else {
            Printer.DebugMessage("Using server_list_response_head_space_v4");
            getserversResponse =
                QueryStrings.GetByteArray(
                    QueryStrings.stringType.server_list_response_head_space_v4);
        }
        // Maybe some have gotten too old since the last query...
        ServerList.Cleanup();
        byte[] gameServerList =
            ServerList.ToByteList(ServerList.GetList(),
                                  localAddress.AddressFamily);
        byte[] eot = QueryStrings.GetByteArray(QueryStrings.stringType.eot);
        byte[] queryResult = Parser.ConcatByteArray(
            new byte[][] {getserversResponse,
                          gameServerList,
                          eot});
        string sendstring = Encoding.ASCII.GetString(queryResult,
                                                     0,
                                                     queryResult.Length);
        Printer.DebugMessage("Sending this: '" + sendstring + "'");
        Printer.DebugMessage("To: " + serverEndpoint);
        listener.Send(queryResult, queryResult.Length, serverEndpoint);
    }

    private static void ProcessIpV6ListRequest(IPEndPoint gameServerEndpoint,
                                               byte[] receivedbytes,
                                               UdpClient listener) {
        Printer.VerboseMessage("---- Received server query request v6 from "
                               + gameServerEndpoint + " ----");
        byte[] serverListQueryHeadV6 =
            QueryStrings.GetByteArray(
                QueryStrings.stringType.server_list_query_head_v6);
        string rest = Encoding.ASCII.GetString(
            receivedbytes.Skip(
                serverListQueryHeadV6.Length).ToArray()).ToLower();
        bool wantFull     = (-1 != rest.IndexOf("full"));
        bool wantEmpty    = (-1 != rest.IndexOf("empty"));
        bool wantOnlyIpv6 = (-1 != rest.IndexOf("ipv6"));
        int protocol = 0;
        string[] parameterList = rest.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        Int32.TryParse(parameterList[0], out protocol);
        // Maybe some have gotten too old since the last query...
        ServerList.Cleanup();
        List<ServerEntry> original = ServerList.GetList();
        List<ServerEntry> filtered = new List<ServerEntry>();
        foreach (ServerEntry originalEntry in original) {
            /* Specialty of IPv6 queries: Client can request limiting the
               results to only IPv6 game servers. Use-case probably: No IPv4 at
               the client side available. So no use for those IPs anyway. */
            bool queryWantsThis =
                (
                    !(   originalEntry.IsEmpty()
                      && !wantEmpty)
                 && !(   originalEntry.IsFull()
                      && !wantFull)
                 && !(wantOnlyIpv6 && originalEntry.IsIpV4()));
            if (   originalEntry.GetProtocol() == protocol
                   && queryWantsThis) {
                filtered.Add(originalEntry);
            }
        }
        byte[] getserversResponse =
            QueryStrings.GetByteArray(
                QueryStrings.stringType.server_list_response_head_v6);
        byte[] gameServerList = ServerList.ToByteList(
            filtered,
            AddressFamily.InterNetworkV6);

        byte[] eot = QueryStrings.GetByteArray(QueryStrings.stringType.eot);
        byte[] query = null;
        query = Parser.ConcatByteArray(new byte[][] {getserversResponse,
                                                     gameServerList,
                                                     eot});
        string sendstring = Encoding.ASCII.GetString(query, 0, query.Length);
        Printer.DebugMessage("Sending this: '" + sendstring + "'");
        listener.Send(query, query.Length, gameServerEndpoint);
    }

    private static void ProcessIpV4ListRequest(IPEndPoint gameServerEndpoint,
                                               byte[] receivedbytes,
                                               UdpClient listener) {
        Printer.VerboseMessage("---- Received server query request v4 from "
                               + gameServerEndpoint + " ----");
        byte[] serverListQueryHeadV4 =
            QueryStrings.GetByteArray(
                QueryStrings.stringType.server_list_query_head_v4);
        string rest = Encoding.ASCII.GetString(
            receivedbytes.Skip(
                serverListQueryHeadV4.Length).ToArray()).ToLower();
        bool wantFull  = (-1 != rest.IndexOf("full"));
        bool wantEmpty = (-1 != rest.IndexOf("empty"));
        int protocol = 0;
        string[] parameterList = rest.Split(
            new [] { ' ' },
            StringSplitOptions.RemoveEmptyEntries);
        Int32.TryParse(parameterList[0], out protocol);
        // Maybe some have gotten too old since the last query...
        ServerList.Cleanup();
        List<ServerEntry> original = ServerList.GetList();
        List<ServerEntry> filtered = new List<ServerEntry>();
        /* Yes, the filtering takes place at the master server, not the
           requesting game client. Back then they really were trying to save
           bandwidth... */
        foreach (ServerEntry originalEntry in original) {
            bool queryWantsThis =
                (
                    !(   originalEntry.IsEmpty()
                      && !wantEmpty)
                 && !(   originalEntry.IsFull()
                      && !wantFull));
            if (   originalEntry.GetProtocol() == protocol
                   && originalEntry.IsIpV4()
                   && queryWantsThis) {
                filtered.Add(originalEntry);
            }
        }
        byte[] getserversResponse =
            QueryStrings.GetByteArray(
                QueryStrings.stringType.server_list_response_head_space_v4);
        byte[] gameServerList = Encoding.ASCII.GetBytes(
            ServerList.ToStringListV4(filtered));
        byte[] eot = QueryStrings.GetByteArray(QueryStrings.stringType.eot);
        byte[] query = null;
        query = Parser.ConcatByteArray(new byte[][] {getserversResponse,
                                                     gameServerList,
                                                     eot});
        string sendstring = Encoding.ASCII.GetString(query, 0, query.Length);
        Printer.DebugMessage("Sending this: '" + sendstring + "'");
        listener.Send(query, query.Length, gameServerEndpoint);
    }

}
