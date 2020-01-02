using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Collections.Generic;

class HeartbeatListener {
    public static void StartListener(ushort listenPort = 27953)
    {
        UdpClient listener = new UdpClient((int)listenPort);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
        if (Masterserver.GetVerbose()) {Console.WriteLine("Starting EF master server ver. {0}...", Masterserver.VersionString);}
        try
        {
            while (true)
            {
                byte[] receivedbytes = listener.Receive(ref groupEP);
                ushort destination_port = (ushort)groupEP.Port;
                if (IsHeartbeatRequest(receivedbytes, destination_port)) {
					if (Masterserver.GetVerbose()) {Console.WriteLine("---- Received heartbeat from {0} ----", groupEP);}
					IPAddress address = groupEP.Address;
					ushort port = (ushort)groupEP.Port;
					ServerEntry new_one = new ServerEntry(address, port);
					ServerList.AddServer(new_one);
					new_one.QueryInfo();
					Masterserver.DebugMessage("Cleaning up...");
					ServerList.Cleanup();
					Masterserver.DebugMessage("New ones protocol: "+ new_one.GetProtocol());
				} else if (IsListRequest(receivedbytes)) {
                    Masterserver.DebugMessage("---- Received server query request from " + groupEP + " ----");
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
						    && !(original_entry.IsEmpty() && !want_empty)
						    && !(original_entry.IsFull() && !want_full)) {
							filtered.Add(original_entry);
						}
					}
					byte[] getserversResponse = QueryStrings.GetArray("server_list_response_head_space");
					byte[] server_list = Encoding.ASCII.GetBytes(ServerList.ToStringList(filtered));
					byte[] eot = QueryStrings.GetArray("eot");
					byte[] query = null;
					query = QueryStrings.ConcatByteArray(new byte[][] {getserversResponse, server_list, eot});
					string sendstring = Encoding.ASCII.GetString(query, 0, query.Length);
					Masterserver.DebugMessage("Sending this: '"+sendstring+"'");
					listener.Send(query, query.Length, groupEP);

				} else {
					Masterserver.DebugMessage("I got that stuff here. Do you recognize any of this?!?");
					Masterserver.DebugMessage(Encoding.ASCII.GetString(receivedbytes));
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

    private static bool IsHeartbeatRequest(byte[] received, ushort port) {
        Masterserver.DebugMessage("IsHeartbeatRequest\nreceived: '"+Encoding.ASCII.GetString(received)+"', port: "+port+"");
		if (received == null) {return false;}
		byte[] heartbeat_signal = QueryStrings.GetHeartbeatComparison(port);
        Masterserver.DebugMessage("comparison: "+Encoding.ASCII.GetString(heartbeat_signal));
		if (received.Length < heartbeat_signal.Length) {return false;}
		return ByteArraysAreEqual(received, heartbeat_signal);
	}
	
	private static bool IsListRequest(byte[] received) {
        Masterserver.DebugMessage("IsListRequest\nreceived: '"+Encoding.ASCII.GetString(received)+"'");
		if (received == null) {return false;}
		byte[] server_list_query_head = QueryStrings.GetArray("server_list_query_head");
        Masterserver.DebugMessage("comparison: "+Encoding.ASCII.GetString(server_list_query_head));
		if (received.Length < server_list_query_head.Length) {return false;}
		return ByteArraysAreEqual(received, server_list_query_head);
	}
	
	private static bool ByteArraysAreEqual (byte[] received, byte[] comparison) {
		int comparisonlength = comparison.Length;
		byte[] left_received = received.Take(comparisonlength).ToArray();
		return left_received.SequenceEqual(comparison);
	}
}
