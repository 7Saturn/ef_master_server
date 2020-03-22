using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

public static class ServerList {
    private static List<ServerEntry> serverlist = null;
    private static Thread CleanupThreadHandle = null;

    public static List<ServerEntry> get_list() {
        if (serverlist == null) {
            serverlist = new List<ServerEntry>();
        }
        if (CleanupThreadHandle == null) {
			CleanupThreadHandle = StartCleanupThread();
		}
        return serverlist;
    }

    public static string ToStringList (List<ServerEntry> original_list) {
        string text_list = "";
        foreach (ServerEntry entry in original_list) {
            if (!entry.ToString().Equals("")) {
                text_list += "\\" + entry.ToString();
            }
        }
        return text_list;
    }

    public static string get_text_list() {
        return ToStringList(ServerList.get_list());
    }

    public static void AddServer(ServerEntry new_one) {
        Printer.DebugMessage("Trying to add " + new_one.ToString());
        if (new_one.ToString().Equals("")) {
            Printer.DebugMessage("Empty server provided for AddServer, skipping this one...");
            return;
        }
        if (!ServerList.get_list().Contains(new_one)) {
            Printer.DebugMessage("A new one arrived, querying data...");
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            new_one.QueryInfo();
            //stopwatch.Stop();
            //Printer.DebugMessage("Time elapsed: " + stopwatch.ElapsedMilliseconds + " ms.");
            //stopwatch.Reset();
            ServerList.get_list().Add(new_one);
            Printer.DebugMessage("Now list looks like this: " + ServerList.get_text_list());
        }
        else {
            Printer.DebugMessage("A known one");
            ServerEntry old_one = ServerList.get_list().Find(x => x.Equals(new_one));
            Printer.DebugMessage("Old protocol: " + old_one.GetProtocol());
            Printer.DebugMessage("New protocol: " + new_one.GetProtocol());
            Printer.DebugMessage("So got to query that one again...");
            old_one.QueryInfo();
        }
    }

    public static void RemoveServer(ServerEntry to_remove) {
        if (ServerList.get_list().Contains(to_remove)) {
            ServerList.get_list().Remove(to_remove);
        }
    }

    public static void Cleanup() {
        Printer.DebugMessage("Cleaning up server list...");
        List<Thread> threadlist = new List<Thread>();
        foreach (ServerEntry serverentry in ServerList.get_list()) {
            Thread thisthread = serverentry.QueryInfoThreaded();
            threadlist.Add(thisthread);
        }
        while (threadlist.Count != 0) {
            List<Thread> residualthreadlist = new List <Thread>();
            foreach(Thread thisthread in threadlist) {
                if (thisthread.IsAlive) {
                    residualthreadlist.Add(thisthread);
                }
            }
            threadlist = residualthreadlist;
        }
        List<ServerEntry> looptemp = ServerList.get_list();
        int number_of_all = ServerList.get_list().Count;
        if (number_of_all == 0) {
            return;
        }

        for (int counter = number_of_all-1; counter >= 0; counter--) {
            if (ServerList.get_list()[counter].GetProtocol() == 0) {
                ServerList.RemoveServer(looptemp[counter]);
            }
        }
        Printer.DebugMessage("Now list looks like this: " + ServerList.get_text_list());
    }

    public static void AddServerListFromMaster(string master_host, ushort master_port) {
        byte[] get_all_servers_query = QueryStrings.GetArray("server_list_all_query_head");

        //First attempt with special dump-all-request server_list_all_query_head
        IPAddress address = NetworkBasics.resolve_host(master_host);
        if (address == null) {
            Console.WriteLine("Hostname {0} could not be resolved, skipping it.", master_host);
            return;
        }
        Printer.DebugMessage("Working on " + master_host + ":" + master_port + ", sending dump all string " + Encoding.ASCII.GetString(get_all_servers_query));
        byte[] receiveBytes = NetworkBasics.GetAnswer(address, ((int)master_port), get_all_servers_query);

        if (receiveBytes == null) {//This is normal, when the master server is not supporting the dump all request (which is usually the case with other master servers)
            Printer.DebugMessage("Received nothing. The server probably does not support the dump all request. Trying it normally with protocols 22 to 24...");
            byte[] server_list_query_head = QueryStrings.GetArray("server_list_query_head");
            byte[] empty = QueryStrings.GetArray("empty");
            byte[] full = QueryStrings.GetArray("full");
            byte[] space = {32};
            byte[] version22 = {50, 50};
            byte[] version23 = {50, 51};
            byte[] version24 = {50, 52};
            byte[] server_list_request22 = QueryStrings.ConcatByteArray(new byte[][] {server_list_query_head, version22, space, full, space, empty});
            byte[] server_list_request23 = QueryStrings.ConcatByteArray(new byte[][] {server_list_query_head, version23, space, full, space, empty});
            byte[] server_list_request24 = QueryStrings.ConcatByteArray(new byte[][] {server_list_query_head, version24, space, full, space, empty});
            byte[][] server_list_requests = {server_list_request22,
                                             server_list_request23,
                                             server_list_request24};
            foreach (byte[] server_list_request in server_list_requests) {
                Printer.DebugMessage("Working on " + master_host + ":" + master_port + ", sending version string " + Encoding.ASCII.GetString(server_list_request));
                receiveBytes = NetworkBasics.GetAnswer(address, ((int)master_port), server_list_request);
                if (receiveBytes == null) {
                    Printer.DebugMessage("Received nothing.");
                    break;
                }
                else {
                    ProcessReceivedListByteArray(receiveBytes);
                }
            }
            Printer.DebugMessage("End of query loop.");
        }
        else { //This works only if the other side knows the dump all request, which is a specialty of this master server you are currently viewing the code of.
            Printer.DebugMessage("Got data via dump all request -> no further queries as we got all we can get");
            ProcessReceivedListByteArray(receiveBytes);
            return;
        }
    }

    private static void ProcessReceivedListByteArray (byte[] receiveBytes) {
        byte[] server_list_answer_head = QueryStrings.GetArray("server_list_response_head");
        byte[] eot = QueryStrings.GetArray("eot");
        Printer.DebugMessage("Received the following:\n" + Encoding.ASCII.GetString(receiveBytes));
        if (receiveBytes.Length < server_list_answer_head.Length) {//+eot.Length
            Printer.DebugMessage("Result is too short.");
            return;
        }
        Byte[] start = receiveBytes.Take(server_list_answer_head.Length).ToArray();
        Byte[]  ende = receiveBytes.Skip(server_list_answer_head.Length + 1).ToArray();
        Byte[]  tail = receiveBytes.Skip(receiveBytes.Length-eot.Length).ToArray();
        Byte[]  data = receiveBytes.Skip(server_list_answer_head.Length + 1).ToArray();
        data = data.Take(data.Length-eot.Length).ToArray();
        while (data.Length > 0 && data[0] == 0) {//stripping leading zeros
            data = data.Skip(1).ToArray();
        }

        if (   start.SequenceEqual(server_list_answer_head)
            && tail.SequenceEqual(eot)) {
            Printer.DebugMessage("Answer is valid.");

            string returnData = Encoding.ASCII.GetString(data);
            Printer.DebugMessage("Data-String: '" + returnData + "'");
            if (returnData.Length > 0) {
                string[] addresses = returnData.Split('\\');
                if (Printer.GetDebug()) {
                    foreach (string adresse in addresses) {
                        Printer.DebugMessage(adresse + " was received");
                    }
                }
                if (ende.SequenceEqual(eot)) {
                    Printer.DebugMessage("But no servers were sent back.");
                }
                else {
                    Printer.DebugMessage("The following servers were returned:");
                    foreach (string address in addresses)
                    {
                        if (!address.Equals("")) {
                            ServerEntry newcomer = new ServerEntry(address);
                            ServerList.AddServer(newcomer);
                        }
                    }
                }
            }
            else {
                Printer.DebugMessage("But apparently the master knows no game servers of that version.");
            }
        }
        else {
            if (Printer.GetDebug()) {
                Console.WriteLine("start:");
                Printer.DumpBytes(start);
                Console.WriteLine("server_list_answer_head:");
                Printer.DumpBytes(server_list_answer_head);
                Console.WriteLine("ende:");
                Printer.DumpBytes(ende);
                Console.WriteLine("tail:");
                Printer.DumpBytes(tail);
                Console.WriteLine("eot:");
                Printer.DumpBytes(eot);
                Console.WriteLine("data:");
                Printer.DumpBytes(data);
            }
            Printer.DebugMessage("Got jibberish here:");
            if (Printer.GetDebug()) {
                Printer.DumpBytes(receiveBytes);
            }
        }
    }

    public static void QueryOtherMasters(string[] masterServerArray) {
        Printer.DebugMessage("Querying provided master servers...");
        foreach (string masterServer in masterServerArray) {
            Printer.DebugMessage("Working on master server '" + masterServer + "'...");
            ushort master_port = 27953;
            string master_host = null;
            if (!masterServer.Equals("")) {
                string[] serverParts = Regex.Split(masterServer, ":");
                int temp_port = 0;
                if (serverParts.Length == 1) {
                    master_host = serverParts[0];
                }
                else if (serverParts.Length == 2) {
                    master_host = serverParts[0];
                    Int32.TryParse(serverParts[1], out temp_port);
                    master_port = (ushort)temp_port;
                }
                else {
                    //IPv6...
                    Printer.DebugMessage("IPv6 address found: " + masterServer);
                    Match parts = Regex.Match(masterServer, @"^\[([0-9,a-f,A-F,:]*)\]");
                    if (parts.Success) {
                        Console.WriteLine("Successor");
                        master_host = parts.Value;
                        Console.WriteLine("IPv6 address derived: " + master_host);
                        MatchCollection blocks = Regex.Matches(masterServer, @"(\d{1,5})$");
                        Console.WriteLine("blocks.Count: {0}", blocks.Count);
                        if (blocks.Count > 0) {
                            string port = blocks[0].Groups[0].ToString();
                            Int32.TryParse(port, out temp_port);
                            master_port = (ushort)temp_port;
                            Console.WriteLine("port: '{0}'", port);
                            Console.WriteLine("master_port: '{0}'", master_port);
                        }
                    }
                    else {
                        Console.WriteLine("no success");
                    }
                }
                if (   master_host != null
                    && master_port != 0) {
                    if (Masterserver.GetVerbose()) {
                        Console.WriteLine("Querying master server '{0}'", masterServer);
                    }
                    ServerList.AddServerListFromMaster(master_host, master_port);
                }
            }
        }
        if (Masterserver.GetVerbose()) {
            Console.WriteLine("Finished querying");
        }
    }

    public static void QueryOtherMastersThreaded(string[] masterServerArray, int interval) {
        Printer.DebugMessage("Querying provided master servers in intervals of " + interval + "s.");
        Thread thread = new Thread(() => ServerList.StartQueryOtherMastersThread(masterServerArray, interval));
        thread.Start();
    }

    private static void StartQueryOtherMastersThread(string[] masterServerArray, int interval) {
        while (true) {
            ServerList.QueryOtherMasters(masterServerArray);
            System.Threading.Thread.Sleep(interval * 1000);
        }
    }

    public static Thread StartCleanupThread() {
        Printer.DebugMessage("Starting cleanup thread.");
        Thread thread = new Thread(() => ServerList.CleanupThread());
        thread.Start();
        return thread;
    }

    public static void StopCleanupThread() {
        Printer.DebugMessage("Stopping cleanup thread.");
        CleanupThreadHandle.Abort();
    }

    private static void CleanupThread() {
        while (true) {
            System.Threading.Thread.Sleep(600000); //Sleep first. Do NOT(!) do it the other way around. If you do this to fast, it might crash on slow or heavily loaded machines, as the list may not exist at first. Once every ten minutes should suffice.
            ServerList.Cleanup();
        }
    }
}
