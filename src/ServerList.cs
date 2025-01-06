using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;

public static class ServerList {
    /* Do not work on this variable directly (no, not even in this class).
       Always use GetList for accessing it. */
    private static List<ServerEntry> serverList = null;
    #if SERVER
        private static Gui observingWindow = null;
    #endif

    /* Make totally, totally certain, that this is called first. As we cannot
       lock it here, and cannot set it earlier, call it in the main thread! */
    public static void InitializeList() {
        if (serverList == null) {
            Printer.DebugMessage("List does not exist, yet. Creating it...");
            serverList = new List<ServerEntry>();
        }
        return;
    }

    public static List<ServerEntry> GetList() {
        if (serverList == null) {
            throw new ListUninitalized("Server list has not been initialized,"
                                       + " yet. Call GetList() only after"
                                       + " initialization via InitializeList()."
                                       + " The latter is done at best before"
                                       + " starting any new threads besides the"
                                       + " main thread, that may access"
                                       + " GetList().");
        }
        return serverList;
    }

    /* It is on purpose, that this is accessible.
       When someone decides to create a temporary list (e.g. filtered original
       list) this should to the same from the outside as from the inside. */
    public static string ToStringListV4(List<ServerEntry> originalList) {
        string textList = "";
        foreach (ServerEntry entry in originalList) {
            if (!entry.ToString().Equals("") && entry.IsIpV4()) {
                textList += "\\" + entry.ToString();
            }
        }
        return textList;
    }

    private static string GetListString() {
        List<ServerEntry> originalList = GetList();
        string textList = "";
        lock(originalList) {
            foreach (ServerEntry entry in originalList) {
                if (entry.IsValid()) {
                    if (entry.IsIpV4()) {
                        textList += "\\";
                    }
                    else {
                        textList += "/";
                    }
                    textList += entry.ToString();
                }
            }
        }
        return textList;
    }

    public static Byte[] ToByteList(List<ServerEntry> originalList,
                                    AddressFamily ipProtocol) {
        Byte[] byteList = new Byte[]{};
        foreach (ServerEntry entry in originalList) {
            Printer.DebugMessage("addressString: " + entry.ToString());
            if (entry.IsIpV4()) {
                Printer.DebugMessage("This game server IP is v4.");
                string addressString = entry.serverEntryinHex();
                if (!addressString.Equals("")) {
                    Byte[] addressBytes = Encoding.ASCII.GetBytes(
                        "\\" + addressString);
                    byteList = Parser.ConcatByteArray(
                        new byte[][] {byteList,
                                      addressBytes});
                }

            }
            if (entry.IsIpV6() && ipProtocol == AddressFamily.InterNetworkV6) {
                Printer.DebugMessage("This game server IP is v6.");
                if (entry.ToByteArray() != null) {
                    byteList = Parser.ConcatByteArray(
                        new byte[][] {byteList,
                            new byte[]{47}, // = /
                            entry.ToByteArray()});
                }
            }
        }
        return byteList;
    }

    public static void AddServer(ServerEntry newOne) {
        Printer.DebugMessage("Trying to add " + newOne.ToString());
        if (newOne.IsIpV6() && !Masterserver.InV6Mode()) {
            /* This case should actually only strike, when successfully querying
               full dumps while in IPv4 mode. Full dumps are, as the name
               suggests, full, possibly also including also IPv6 hosts.
               We don't do this, but if someone chooses to do so...
               If querying normally, then it's an IPv4 mode query, which should
               never give IPv6 hosts. */
            Printer.DebugMessage("Is an IPv6 game server, but we are not in"
                                 + " IPv6 mode, so not adding this one.");
        }
        if (!newOne.ReadyToQuery()) {
            Printer.DebugMessage("Empty server provided for AddServer, skipping"
                                 + " this one...");
            return;
        }
        Printer.DebugMessage("Locking list.");

        // At worst we have two or three threads trying to access this list.
        // Let's lock it for us!
        lock(GetList()) {
            Printer.DebugMessage("Locked list.");
            if (!GetList().Contains(newOne)) {
                Printer.DebugMessage("A new one arrived, querying data...");
                newOne.QueryInfo();
                GetList().Add(newOne);
                Printer.DebugMessage("Now list looks like this: "
                                     + GetListString());
            }
            else {
                Printer.DebugMessage("A known one, looking up the old one and"
                                     + " querying it again...");
                ServerEntry oldOne = GetList().Find(x => x.Equals(newOne));
                /* Worst case: v6 thread and v4 thread are both working on the
                   same, known to both, server. So better locking it. */
                lock (oldOne) {
                    Printer.DebugMessage("Old protocol: "
                                         + oldOne.GetProtocol());
                    oldOne.QueryInfo();
                }
            }
            Printer.DebugMessage("Unlocking list.");
        }
        /* We used to do a RefreshGuiList() here, after successfully querying.
           Don't do that. If someone spams us with heartbeats, the gui will fail
           to catch up and freeze after a few rounds of spamming. */
    }

    public static void RemoveServer(ServerEntry toRemove) {
        #if SERVER
            // At worst we have two or three threads trying to access this list.
            // Let's lock it for us!
            lock(GetList()) {
                if (GetList().Contains(toRemove)) {
                    GetList().Remove(toRemove);
                }
            }
            /* We used to do a RefreshGuiList() here, after successfully
               querying. Don't do that. If someone spams us with heartbeats, the
               gui will fail to catch up and freeze after a few rounds of
               spamming. */
        #endif
    }

    public static void Cleanup() {
        Printer.DebugMessage("Cleaning up server list...");
        // At worst we have two or three threads trying to access this list.
        // Let's lock it for us!
        lock(GetList()) {
            foreach (ServerEntry serverentry in GetList()) {
                serverentry.InvalidateIfTooOld();
            }
            List<ServerEntry> looptemp = GetList();
            int numberOfAllServers = GetList().Count;

            /* Going backwards to ensure we never run over any record twice and
               still miss none and also never go too far. */
            for (int counter = numberOfAllServers - 1;
                 counter >= 0;
                 counter--) {
                if (!GetList()[counter].IsValid()) {
                    Printer.DebugMessage(GetList()[counter].ToString()
                                         + " is no longer valid, removing from"
                                         + " list altogether...");
                    RemoveServer(looptemp[counter]);
                }
            }
        }
        /* We used to do a RefreshGuiList() here, after successfully querying.
           Don't do that. If someone spams us with heartbeats, the gui will fail
           to catch up and freeze after a few rounds of spamming. */
        Printer.DebugMessage("Now list looks like this: " + GetListString());
    }

    public static void AddServerListFromMasterHost(string foreignMasterHost,
                                                   ushort foreignMasterPort) {
        Printer.DebugMessage("AddServerListFromMasterHost for "
                             + foreignMasterHost + ":" + foreignMasterPort);
        Printer.DebugMessage("Resolving this host: " + foreignMasterHost);
        IPAddress[] addressesV4 =
            NetworkBasics.ResolveHosts(foreignMasterHost,
                                       AddressFamily.InterNetwork);
        IPAddress[] addressesV6 =
            NetworkBasics.ResolveHosts(foreignMasterHost,
                                       AddressFamily.InterNetworkV6);
        if (addressesV4 == null) {
            Printer.DebugMessage("v4 could not be resolved");
        }
        else {
            Printer.DebugMessage("Resolved to v4:");
            foreach (IPAddress IP in addressesV4) {
                Printer.DebugMessage(IP.ToString());
            }
        }
        if (addressesV6 == null) {
            Printer.DebugMessage("v6 could not be resolved");
        }
        else {
            Printer.DebugMessage("Resolved to v6:");
            foreach (IPAddress IP in addressesV6) {
                Printer.DebugMessage(IP.ToString());
            }
        }
        if (addressesV4 == null && addressesV6 == null) {
            Console.WriteLine("Hostname {0} could not be resolved at all,"
                              + " skipping it.", foreignMasterHost);
            return;
        }

        /* v6 master servers will support v4, but not the other way around.
           v6 returns *all* known servers (when queried with �ipv6�),
           So if we fail here, we might still succeed with v4.
           But if we succeed here, we will not need to check v4 as well, as v6
           replies will already contain the known v4 addresses as well.
           So we prefer V6 over V4.
           Dump all is our specialty and should work on v4 and v6 likewise.
           So no EF protocol distinction required for v6 dump requests.
           At worst, it is an older version of our master server software, that
           only speaks v4. But it will return v4 stuff properly. */
        if (   !Masterserver.InV6Mode()
            && addressesV6 != null
            && addressesV4 == null) {
            Console.WriteLine("Given master server '{0}' seems to be IPv6 only."
                              + " Our master server does not run in IPv6 mode."
                              + " Either set --v6mode switch or remove the IPv6"
                              + " master server to copy from. Skipping it.",
                              foreignMasterHost);
            return;
        }
        bool gotResults = false;
        if (Masterserver.InV6Mode() && addressesV6 != null) {
            foreach (IPAddress addressV6 in addressesV6) {
                if (gotResults) {
                    Printer.DebugMessage("IPv6 already gave us something."
                                         + " Finished here.");
                    return;
                }
                Printer.DebugMessage("Trying our luck on ["
                                     + addressV6.ToString() + "]:"
                                     + foreignMasterPort + "...");
                gotResults = AddServerListFromMasterV6(foreignMasterHost,
                                                       foreignMasterPort,
                                                       addressV6);
            }
        }
        if (gotResults) {
            Printer.DebugMessage("IPv6 already gave us something. Finished"
                                 + " here.");
            return;
        }
        /* OK, we apparently either received nothing from the IPv6 host, or we
           (intentionally) didn't even try. Leaves only IPv4. */
        if (addressesV4 == null && !gotResults) {
            Printer.DebugMessage("IPv6 didn't give us a thing, but we don't"
                                 + " have an IPv4 either. Nothing to be done.");
            return;
        }
        foreach (IPAddress addressV4 in addressesV4) {
            if (gotResults) {
                Printer.DebugMessage("IPv4 gave us something. Finished here.");
                return;
            }
            Printer.DebugMessage("Trying our luck on " + addressV4.ToString()
                                 + ":" + foreignMasterPort + "...");
            gotResults = AddServerListFromMasterV4(foreignMasterHost,
                                                   foreignMasterPort,
                                                   addressV4);
        }

        Printer.DebugMessage("We tried our best, but the IPv4 server didn't"
                             + " give us anything sensible. (And I mean"
                             + " nothing, no correct reply, not just no"
                             + " servers.) Are you sure there's a master server"
                             + " listening here?");
    }

    // Return value indicates whether it got any data from it.
    private static bool AddServerListFromMasterV6(string foreignMasterHost,
                                                  ushort foreignMasterPort,
                                                  IPAddress address) {
        Printer.DebugMessage("Trying to query v6 master...");
        byte[] getAllServersQuery = QueryStrings.GetByteArray(QueryStrings.stringType.server_list_all_query_head);
        Printer.VerboseMessage("Requesting game server list from "
                               + foreignMasterHost + ":" + foreignMasterPort
                               + " (v6)...");
        Printer.DebugMessage("Sending string " + Encoding.ASCII.GetString(getAllServersQuery));
        byte[] receiveBytes = NetworkBasics.GetAnswer(address,
                                                      ((int)foreignMasterPort),
                                                      getAllServersQuery);
        if (receiveBytes == null) {
            /* This is normal, when the master server is not supporting the dump
               all request (which is usually the case with other master servers) */
            Printer.DebugMessage("Received nothing. The server probably does"
                                 + " not support the dump all request.");
            /* There are no game clients or servers with communications protocol
               22 or 23 that support IPv6. So there are no IPv6 clients querying
               or servers announcing 22 or 23 here anyway. So we can skip those
               and concentrate on 24 alone: */
            Printer.DebugMessage("IPv6 mode and IPv6 master server: Querying"
                                 + " only protocol 24.");
            byte[] serverListRequestV6 = QueryStrings.GetServerListRequestV6();
            Printer.DebugMessage("Working on " + foreignMasterHost + ":"
                                 + foreignMasterPort + ", sending string "
                                 + Encoding.ASCII.GetString(serverListRequestV6));
            receiveBytes = NetworkBasics.GetAnswer(address,
                                                   ((int)foreignMasterPort),
                                                   serverListRequestV6);
            if (receiveBytes == null) {
                Printer.VerboseMessage("Received nothing.");
                return false;
            }
            else {
                ProcessReceivedListByteArray(receiveBytes);
                return true;
            }
        }
        else {
            /* This works only if the other side knows the dump all request,
               which is a specialty of this master server you are currently
               viewing the code of. */
            Printer.DebugMessage("Got data via dump all request -> no further"
                                 + " queries as we got all we can get");
            ProcessReceivedListByteArray(receiveBytes);
            return true;
        }
    }

    private static bool AddServerListFromMasterV4(string foreignMasterHost,
                                                  ushort foreignMasterPort,
                                                  IPAddress address) {
        Printer.DebugMessage("Trying to query v4 master...");
        byte[] getAllServersQuery = QueryStrings.GetByteArray(
            QueryStrings.stringType.server_list_all_query_head);
        Printer.VerboseMessage("Requesting game server list from "
                               + foreignMasterHost + ":" + foreignMasterPort
                               + " (v4)...");
        Printer.DebugMessage("Sending string "
                             + Encoding.ASCII.GetString(getAllServersQuery));
        byte[] receiveBytes = NetworkBasics.GetAnswer(address,
                                                      ((int)foreignMasterPort),
                                                      getAllServersQuery);
        bool alreadyGotSomething = false;
        if (receiveBytes == null) {
            /* This is normal, when the master server is not supporting the dump
               all request (which is usually the case with other master servers) */
            Printer.DebugMessage("Received nothing. The server probably does"
                                 + " not support the dump all request.");
            Printer.DebugMessage("IPv4 mode: Querying all three protocols.");
            byte[][] serverListRequestsV4 = QueryStrings.GetServerListRequestsV4();
            foreach (byte[] currentVersionServerListRequest in serverListRequestsV4) {
                Printer.DebugMessage("Working on " + foreignMasterHost + ":"
                                     + foreignMasterPort
                                     + ", sending version string "
                                     + Encoding.ASCII.GetString(
                                         currentVersionServerListRequest));
                receiveBytes = NetworkBasics.GetAnswer(
                    address,
                    ((int)foreignMasterPort),
                    currentVersionServerListRequest);
                if (receiveBytes == null) {
                    Printer.DebugMessage("Received nothing. It should at least"
                                         + " have given us an empty list."
                                         + " Assuming it is not responding, so"
                                         + " skipping remaining protocols (if"
                                         + " any).");
                    return alreadyGotSomething;
                }
                else {
                    ProcessReceivedListByteArray(receiveBytes);
                    alreadyGotSomething = true;
                }
            }
            Printer.DebugMessage("End of query loop.");
            return alreadyGotSomething;
        }
        else {
            /* This works only if the other side knows the dump all request,
               which is a specialty of this master server you are currently
               viewing the code of. */
            Printer.DebugMessage("Got data via dump all request -> no further"
                                 + " queries as we got all we can get");
            ProcessReceivedListByteArray(receiveBytes);
            return true;
        }
    }

    private static void ProcessReceivedListByteArray(byte[] receiveBytes) {
        /* So we requested a list of IPs. Depending on how we requested (v6 or
           v4), the answer can have both heads: */
        byte[] serverListAnswerHeadV4 = QueryStrings.GetByteArray(
            QueryStrings.stringType.server_list_response_head_v4);
        byte[] serverListAnswerHeadV6 = QueryStrings.GetByteArray(
            QueryStrings.stringType.server_list_response_head_v6);

        byte[] eot = QueryStrings.GetByteArray(QueryStrings.stringType.eot);
        Printer.DebugMessage("Received the following:\n" + Encoding.ASCII.GetString(receiveBytes));
        if (receiveBytes.Length < serverListAnswerHeadV4.Length + eot.Length) {
            Printer.VerboseMessage("Result is too short.");
            return;
        }
        bool isV6 = false;
        bool isV4 = false;
        bool cannotBeV6 = receiveBytes.Length < serverListAnswerHeadV6.Length
            + eot.Length;
        Byte[] listHead = null;
        Byte[] listBody = null;
        if (cannotBeV6) {
            Printer.DebugMessage("Result is too short for v6.");
            listHead =
                receiveBytes.Take(serverListAnswerHeadV4.Length).ToArray();
            listBody =
                receiveBytes.Skip(serverListAnswerHeadV4.Length).ToArray();
        }
        else {
            listHead = receiveBytes.Take(serverListAnswerHeadV6.Length).ToArray();
            if (Encoding.ASCII.GetString(listHead).Equals(
                    Encoding.ASCII.GetString(serverListAnswerHeadV6))) {
                Printer.DebugMessage("Is actual v6 header.");
                isV6 = true;
                listBody =
                    receiveBytes.Skip(serverListAnswerHeadV6.Length).ToArray();
            }
            else {
                listHead =
                    receiveBytes.Take(serverListAnswerHeadV4.Length).ToArray();
                listBody =
                    receiveBytes.Skip(serverListAnswerHeadV4.Length).ToArray();
            }
        }
        isV4 = Encoding.ASCII.GetString(listHead).Equals(Encoding.ASCII.GetString(serverListAnswerHeadV4));
        if (isV4) {
            Printer.DebugMessage("Is actual v4 header.");
        }
        Byte[]  listFoot = receiveBytes.Skip(receiveBytes.Length - eot.Length).ToArray();
        // Skipping Header.
        Byte[] data = null;
        if (isV6) {
            data = receiveBytes.Skip(serverListAnswerHeadV6.Length).ToArray();
        }
        else {
            data = receiveBytes.Skip(serverListAnswerHeadV4.Length).ToArray();
        }
        // Removing Footer.
        data = data.Take(data.Length - eot.Length).ToArray();

        // Stripping leading zeros and spaces.
        // Some master servers deviate from the form which works for the clients.
        // Should work for us, too.
        // This will do nothing for V6, as V6 has no leading zeros or spaces.
        while (data.Length > 0 && (data[0] == 0 || data[0] == 32)) {
            data = data.Skip(1).ToArray();
        }
        bool validAnswer = (isV4 || isV6) && listFoot.SequenceEqual(eot);
        if (validAnswer) {
            Printer.DebugMessage("Answer is valid.");
            /* The header and footer look good. Now let's work on the enclosed
               server strings. */
            uint serversCount = addGameServersFromQueryString(data);
            if (serversCount == 0) {
                Printer.DebugMessage("But apparently the master knows no game"
                                     + " servers of that version.");
            }
        }
        else {
            if (Printer.GetDebug()) {
                Console.WriteLine("listHead:");
                Printer.DumpBytes(listHead);
                Console.WriteLine("serverListAnswerHeadV4:");
                Printer.DumpBytes(serverListAnswerHeadV4);
                Console.WriteLine("serverListAnswerHeadV6:");
                Printer.DumpBytes(serverListAnswerHeadV6);
                Console.WriteLine("listBody:");
                Printer.DumpBytes(listBody);
                Console.WriteLine("listFoot:");
                Printer.DumpBytes(listFoot);
                Console.WriteLine("eot:");
                Printer.DumpBytes(eot);
                Console.WriteLine("data:");
                Printer.DumpBytes(data);
            }
            Printer.VerboseMessage("Got jibberish here:");
            if (Printer.GetDebug()) {
                Printer.DumpBytes(receiveBytes);
            }
        }
    }

    private static uint addGameServersFromQueryString(Byte[] gameServerListBytes) {
        /* Remember: v4 addresses were always prepended with a \
                     while v6 addresses will be prepended by a /,
                     but only if it actually is a v6 master server
           That's why we have to do that step by step for v6. */

        uint serversCount = 0;
        bool bsFound = false;
        /* Footer and header are already gone here.
           So this can only mean, payload. */
        while (gameServerListBytes.Length > 0 && !bsFound) {
            Printer.DebugMessage("Data-String: '" +
                                 Encoding.ASCII.GetString(gameServerListBytes)
                                 + "'");
            byte gameServerHead = gameServerListBytes[0];
            if (gameServerHead == 92) { // =    \
                // v4 case
                gameServerListBytes = gameServerListBytes.Skip(1).ToArray();
                Printer.DebugMessage("Next record should be an IPv4 server.");
                /* Servers are 8 Bytes Hex Digits per IP
                             + 4 Bytes Hex Digits for the Port.
                   So block size is 12 bytes per server record. */
                if (gameServerListBytes.Length < 12) {
                    Printer.DebugMessage(
                        "That's odd. This server is too short: "
                        + Encoding.ASCII.GetString(gameServerListBytes));
                    Printer.DebugMessage(
                        "Ignoring the BS, but ending"
                        + " processing the received data, as"
                        + " more problems are to be expected...");
                    return serversCount;
                }
                Byte[] ipV4Server = gameServerListBytes.Take(12).ToArray();
                gameServerListBytes = gameServerListBytes.Skip(12).ToArray();
                ServerEntry newcomer = new ServerEntry(ipV4Server);
                AddServer(newcomer);
                serversCount++;
            }
            if (gameServerHead == 47) {// = /
                // v6 case
                gameServerListBytes = gameServerListBytes.Skip(1).ToArray();
                Printer.DebugMessage("Next record should be an IPv6 server.");
                // Servers are 16 Bytes long + 2 Bytes Port,
                // So block size is 18 bytes per server record.
                if (gameServerListBytes.Length < 18) {
                    Printer.DebugMessage(
                        "That's odd. This server is too short: "
                        + Encoding.ASCII.GetString(gameServerListBytes));
                    Printer.DebugMessage("Ignoring the BS, but ending"
                                         + " processing the received data...");
                    return serversCount;
                }
                Byte[] ipV6Server = gameServerListBytes.Take(18).ToArray();
                gameServerListBytes = gameServerListBytes.Skip(18).ToArray();
                ServerEntry newcomer = new ServerEntry(ipV6Server);
                AddServer(newcomer);
                serversCount++;
            }
            if (gameServerHead != 92 && gameServerHead != 47) { // =    \
                Printer.DebugMessage("Parsing problem: The list item lacks a"
                                     + " proper head ('/' or '\' required,"
                                     + " ASCII "
                                     + gameServerHead + " was found).");
                bsFound = true;
            }
        }
        Printer.DebugMessage("Reached end of list.");
        return serversCount;
    }

    public static void QueryOtherMasters(string[] masterServerNames) {
        Printer.DebugMessage("Querying provided master servers...");
        foreach (string masterServerName in masterServerNames) {
            Printer.DebugMessage("Working on master server '"
                                 + masterServerName + "'...");
            ushort foreignMasterPort = 27953;
            // May contain a real host name or an IP, v4 or v6:
            string foreignMaster = null;
            if (!masterServerName.Equals("")) {
                // Assumption: Hostname or Hostname:Port
                string[] serverParts = Regex.Split(masterServerName, ":");
                int temporaryPort = 0;
                if (serverParts.Length == 1) { // Only host name
                    foreignMaster = serverParts[0];
                }
                else if (serverParts.Length == 2) { // host name and port
                    foreignMaster = serverParts[0];
                    Int32.TryParse(serverParts[1], out temporaryPort);
                    foreignMasterPort = (ushort)temporaryPort;
                }
                else { // many :, is probably an IPv6 address.
                    Printer.DebugMessage("Suspected IPv6 address found: "
                                         + masterServerName);
                    if (Masterserver.InV6Mode()) {
                        /* Problem is:
                           From [::1] to [123:45:6789::abcd] to
                           [1234:5678:90ab:cdef:0123:4567:890a:bcde]:12345
                           everything is possible
                           That's kinda tricky.
                           in terms of blocks between :...
                           Only thing we are sure of is: The IP _has_ to be
                           between [] and there must be at least two :
                           Hex digits are all used. But nothing more. */
                        Match parts = Regex.Match(masterServerName,
                                                  @"^\[([0-9,a-f,A-F,:]*)\]");
                        if (parts.Success) {
                            foreignMaster = parts.Value;
                            Printer.DebugMessage("IPv6 address derived: "
                                                 + foreignMaster);
                            /* Assumption: The port is always outside the
                               brackets but separated by the :...]:<port> */
                            MatchCollection blocks =
                                Regex.Matches(masterServerName,
                                              @":(\d{1,5})$");
                            if (blocks.Count > 0) {
                                blocks = Regex.Matches(masterServerName,
                                                       @"(\d{1,5})$");
                                string port = blocks[0].Groups[0].ToString();
                                Int32.TryParse(port, out temporaryPort);
                                foreignMasterPort = (ushort)temporaryPort;
                                Printer.DebugMessage("port: '" + port + "'");
                                Printer.DebugMessage("foreignMasterPort: '"
                                                     + foreignMasterPort + "'");
                            }
                        }
                        else {
                            Console.Error.WriteLine("Could not parse master"
                                                    + " server '{0}'."
                                                    + " Aborting.",
                                                    masterServerName);
                            Environment.Exit(1);
                        }
                    }
                    else {
                        Console.Error.WriteLine("Given master server IP '{0}'"
                                                + " seems to be IPv6. Our"
                                                + " master server does not run"
                                                + " in IPv6 mode. Either set"
                                                + " --v6mode switch or remove"
                                                + " the IPv6 from --copy-from.",
                                                masterServerName);
                        Environment.Exit(1);
                    }
                }
                if (   foreignMaster != null
                    && foreignMasterPort != 0) {
                    Printer.VerboseMessage("Querying master server '"
                                           + masterServerName + "'");
                    AddServerListFromMasterHost(foreignMaster,
                                                foreignMasterPort);
                }
            }
        }
        Cleanup();
        Printer.VerboseMessage("Finished querying");
    }

    public static Thread QueryOtherMastersThreaded(string[] masterServerNames,
                                                   int interval) {
        Printer.DebugMessage("Querying provided master servers in intervals of "
                             + interval + "s.");
        Thread thread = new Thread(() => StartQueryOtherMastersThread(
                                       masterServerNames,
                                       interval));
        thread.Start();
        return thread;
    }

    private static void StartQueryOtherMastersThread(string[] masterServerNames,
                                                     int interval) {
        while (true) {
            QueryOtherMasters(masterServerNames);
            System.Threading.Thread.Sleep(interval * 1000);
        }
    }
    #if SERVER
        public static void RegisterObserver(Gui mainWindow) {
            Printer.DebugMessage("RegisterObserver");
            if (observingWindow == null) {
                observingWindow = mainWindow;
            }
        }

        public static void DeregisterObserver() {
            Printer.DebugMessage("DeregisterObserver");
            observingWindow = null;
        }
    #endif

    private static void RefreshGuiList() {
        #if SERVER
            Printer.DebugMessage("RefreshGuiList from Server List");
            if (observingWindow != null) {
                Printer.DebugMessage("Actually doing RefreshGuiList");
                observingWindow.RefreshSafe();
            }
        #endif
    }
}
