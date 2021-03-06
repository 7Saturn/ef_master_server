using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;

class HeartbeatListener {

    private static Thread listenerThreadHandle;

    public static Thread StartListenerThread(IPAddress localAddress, ushort listenPort = 27953) {
        Printer.DebugMessage("Starting listener thread.");
        listenerThreadHandle = new Thread(() => StartListener(localAddress, listenPort));
        listenerThreadHandle.Start();
        return listenerThreadHandle;
    }

    public static void StopListenerThread() {
        Printer.DebugMessage("Stopping listener thread.");
        listenerThreadHandle.Abort();
    }

    public static void StartListener(IPAddress localAddress, ushort listenPort = 27953) {
        Printer.DebugMessage("StartListener");
        if (localAddress == null) {
            Printer.VerboseMessage("Got no interface set, listening on any available interface...");
            localAddress = IPAddress.Any;
        }
        Printer.DebugMessage("before endpoint.");
        IPEndPoint groupEP = new IPEndPoint(localAddress, listenPort);
        Printer.DebugMessage("after endpoint.");
        UdpClient listener = null;
        try {
            listener = new UdpClient(groupEP);
        }
        catch (SocketException e) {
            if (e.ErrorCode == 10049) {
                Console.WriteLine("Could not bind server to IP {0}. Are you sure, this is a proper IPv4 address of a local network interface?", localAddress);
            }
            else {
                Console.WriteLine("Something unexpected happend, while trying to open local IP end point:\n" + e.ToString());
            }
            Environment.Exit(1);
        }
        try
        {
            Printer.DebugMessage("Entered main listener loop.");
            while (true)
            {
                byte[] receivedbytes = listener.Receive(ref groupEP);
                ushort destination_port = (ushort)groupEP.Port;
                if (IsHeartbeatRequest(receivedbytes, destination_port)) {
                    Printer.VerboseMessage("---- Received heartbeat from " + groupEP + " ----");
                    IPAddress address = groupEP.Address;
                    ushort port = (ushort)groupEP.Port;
                    ServerEntry new_one = new ServerEntry(address, port);
                    ServerList.AddServer(new_one);
                    new_one.QueryInfo();
                    Printer.DebugMessage("New ones protocol: " + new_one.GetProtocol());
                }
                else if (IsListRequest(receivedbytes)) {
                    Printer.VerboseMessage("---- Received server query request from " + groupEP + " ----");
                    ServerList.Cleanup();
                    byte[] server_list_query_head = QueryStrings.GetArray("server_list_query_head");
                    string rest = Encoding.ASCII.GetString(receivedbytes.Skip(server_list_query_head.Length).ToArray()).ToLower();
                    bool want_full = false;
                    bool want_empty = false;
                    int protocol = 0;
                    if (-1 != rest.IndexOf("full")) {want_full = true;}
                    if (-1 != rest.IndexOf("empty")) {want_empty = true;}
                    string[] parameter_list = rest.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    Int32.TryParse(parameter_list[0], out protocol);
                    List<ServerEntry> original = ServerList.get_list();
                    List<ServerEntry> filtered = new List<ServerEntry>();
                    foreach (ServerEntry original_entry in original) {
                        if (   original_entry.GetProtocol() == protocol
                            && !(   original_entry.IsEmpty()
                                 && !want_empty)
                            && !(   original_entry.IsFull()
                                 && !want_full)) {
                            filtered.Add(original_entry); //yes, the filtering takes place at the master server, not the requesting game client. Back then they really were trying to save bandwidth...
                        }
                    }
                    byte[] getserversResponse = QueryStrings.GetArray("server_list_response_head_space");
                    byte[] server_list = Encoding.ASCII.GetBytes(ServerList.ToStringList(filtered));
                    byte[] eot = QueryStrings.GetArray("eot");
                    byte[] query = null;
                    query = QueryStrings.ConcatByteArray(new byte[][] {getserversResponse, server_list, eot});
                    string sendstring = Encoding.ASCII.GetString(query, 0, query.Length);
                    Printer.DebugMessage("Sending this: '" + sendstring + "'");
                    listener.Send(query, query.Length, groupEP);

                }
                else if (IsDumpRequest(receivedbytes)) {//not standard issue. Original EF did never know/support this, but it makes querying other masters for the purpose of running a master yourself a lot easier/faster
                    Printer.DebugMessage("---- Received server dump query request from " + groupEP + " ----");
                    ServerList.Cleanup();
                    byte[] getserversResponse = QueryStrings.GetArray("server_list_response_head_space");
                    byte[] server_list = Encoding.ASCII.GetBytes(ServerList.ToStringList(ServerList.get_list()));
                    byte[] eot = QueryStrings.GetArray("eot");
                    byte[] query_result = QueryStrings.ConcatByteArray(new byte[][] {getserversResponse, server_list, eot});
                    string sendstring = Encoding.ASCII.GetString(query_result, 0, query_result.Length);
                    Printer.DebugMessage("Sending this: '" + sendstring + "'");
                    listener.Send(query_result, query_result.Length, groupEP);

                }
                else {
                    Printer.DebugMessage("I got that stuff here. Do you recognize any of this?!?");
                    Printer.DebugMessage(Encoding.ASCII.GetString(receivedbytes));
                }
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine(e);
            Console.WriteLine("Sorry, cannot start the master server...");
            Environment.Exit(1);
        }
        finally
        {
            listener.Close();
        }
    }
    //It is not enough to simply compare the Strings! e. g. the four 0x255 characters turn into question marks, which in turn would fit them, although they are not the same. So using byte arrays, that works properly.
    private static bool IsHeartbeatRequest(byte[] received, ushort port) {
        Printer.DebugMessage("IsHeartbeatRequest\nreceived: '" + Encoding.ASCII.GetString(received) + "', port: " + port);
		if (received == null) {return false;}
		byte[] heartbeat_signal = QueryStrings.GetHeartbeatComparison(port);
        Printer.DebugMessage("comparison: " + Encoding.ASCII.GetString(heartbeat_signal));
		if (received.Length < heartbeat_signal.Length) {return false;}
		return ByteArraysAreEqual(received, heartbeat_signal);
	}

	private static bool IsListRequest(byte[] received) {
        Printer.DebugMessage("IsListRequest\nreceived: '" + Encoding.ASCII.GetString(received) + "'");
		if (received == null) {return false;}
		byte[] server_list_query_head = QueryStrings.GetArray("server_list_query_head");
        Printer.DebugMessage("comparison: " + Encoding.ASCII.GetString(server_list_query_head));
		if (received.Length < server_list_query_head.Length) {return false;}
		return ByteArraysAreEqual(received, server_list_query_head);
	}

	private static bool IsDumpRequest(byte[] received) {
        Printer.DebugMessage("IsDumpRequest\nreceived: '" + Encoding.ASCII.GetString(received) + "'");
		if (received == null) {return false;}
		byte[] server_list_all_query_head = QueryStrings.GetArray("server_list_all_query_head");
        Printer.DebugMessage("comparison: " + Encoding.ASCII.GetString(server_list_all_query_head));
		if (received.Length < server_list_all_query_head.Length) {return false;}
		return ByteArraysAreEqual(received, server_list_all_query_head);
	}

	private static bool ByteArraysAreEqual (byte[] received, byte[] comparison) {
		int comparisonlength = comparison.Length;
		byte[] left_received = received.Take(comparisonlength).ToArray();
		return left_received.SequenceEqual(comparison);
	}
}
