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
        foreach (ServerEntry eintrag in original_list) {
            if (!eintrag.ToString().Equals("")) {
                text_list += "\\"+eintrag.ToString();
            }
        }
        return text_list;

    }

    public static string get_text_list() {
        return ToStringList(ServerList.get_list());
    }

    public static void AddServer(ServerEntry new_one) {
        Masterserver.DebugMessage("Trying to add "+new_one.ToString());
        if (!ServerList.get_list().Contains(new_one)) {
            Masterserver.DebugMessage("A new one arrived, querying data...");
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            new_one.QueryInfo();
            //stopwatch.Stop();
            //Masterserver.DebugMessage("Time elapsed: "+stopwatch.ElapsedMilliseconds+"ms.");
            //stopwatch.Reset();
            ServerList.get_list().Add(new_one);
            Masterserver.DebugMessage("Now list looks like this: "+ServerList.get_text_list());
        } else {
            Masterserver.DebugMessage("A known one");
            ServerEntry old_one = ServerList.get_list().Find(x => x.Equals(new_one));
            Masterserver.DebugMessage("Old protocol: "+old_one.GetProtocol());
            Masterserver.DebugMessage("New protocol: "+new_one.GetProtocol());
            Masterserver.DebugMessage("So got to query that one again...");
            old_one.QueryInfo();
        }
    }

    public static void RemoveServer(ServerEntry to_remove) {
        if (ServerList.get_list().Contains(to_remove)) {
            ServerList.get_list().Remove(to_remove);
        }
    }

    public static void Cleanup() {
        Masterserver.DebugMessage("Cleaning up server list...");
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

        for (int counter = number_of_all-1;counter >= 0;counter--) {
            if (ServerList.get_list()[counter].GetProtocol() == 0) {
                ServerList.RemoveServer(looptemp[counter]);
            }
        }
        Masterserver.DebugMessage("Now list looks like this: "+ServerList.get_text_list());
    }

    public static void AddServerListFromMaster(string master_host, ushort master_port) {
        byte[] server_list_query_head = QueryStrings.GetArray("server_list_query_head");
        byte[] empty = QueryStrings.GetArray("empty");
        byte[] full = QueryStrings.GetArray("full");
        byte[] eot = QueryStrings.GetArray("eot");
        byte[] space = {32};
        byte[] version0 = {48};
        byte[] version22 = {50, 50};
        byte[] version23 = {50, 51};
        byte[] version24 = {50, 52};
        byte[] server_list_request00 = QueryStrings.ConcatByteArray(new byte[][] {server_list_query_head, version0,  space, full, space, empty}); //Only useful with this master server version also on the other side: Gives /all/ known servers back from the other master server
        byte[] server_list_request22 = QueryStrings.ConcatByteArray(new byte[][] {server_list_query_head, version22, space, full, space, empty});
        byte[] server_list_request23 = QueryStrings.ConcatByteArray(new byte[][] {server_list_query_head, version23, space, full, space, empty});
        byte[] server_list_request24 = QueryStrings.ConcatByteArray(new byte[][] {server_list_query_head, version24, space, full, space, empty});
        //server_list_request00 = server_list_request24;
        byte[][] server_list_requests = {server_list_request00, server_list_request22, server_list_request23, server_list_request24};
        byte[] server_list_answer_head = QueryStrings.GetArray("server_list_response_head");

        foreach (byte[] server_list_request in server_list_requests) {
            Masterserver.DebugMessage("Working on " + master_host + ":" + master_port + ", sending version string " + Encoding.ASCII.GetString(server_list_request));
            IPAddress address;
            try {
                address = IPAddress.Parse(master_host);
            }
            catch (Exception e) {
                Masterserver.DebugMessage(e.ToString());
                Masterserver.DebugMessage(master_host+" is not a valid IP address. Trying to resolve it, assuming it is a host name...");
                try {
                    IPAddress[] ipaddresses = Dns.GetHostAddresses(master_host);
                    List<IPAddress> temp_ipaddresses = new List<IPAddress>();
                    foreach (IPAddress ipaddress in ipaddresses) {
                        if (ipaddress.AddressFamily.ToString() == ProtocolFamily.InterNetwork.ToString()) {
                            temp_ipaddresses.Add(ipaddress);
                        }
                    }
                    ipaddresses = temp_ipaddresses.ToArray();
                    if (ipaddresses.Length > 0) {
                        address = ipaddresses[0];
                    } else {
                        Masterserver.DebugMessage("Could not resolve "+master_host+" to a valid IP address.");
                        break;
                    }
                    Masterserver.DebugMessage("Resolved "+master_host+" to "+address.ToString()+".");
                }
                catch (Exception e2) {
                    Masterserver.DebugMessage(e2.ToString());
                    Masterserver.DebugMessage("Could not resolve "+master_host+" to a valid IP address.");
                    break;
                }
            }
            byte[] receiveBytes = NetworkBasics.GetAnswer(address, ((int)master_port), server_list_request);
            if (receiveBytes == null) {
                Masterserver.DebugMessage("Received nothing.");
                if (server_list_request != server_list_request00) {
					break;
				}
            } else {
				Masterserver.DebugMessage("Received the following:\n"+Encoding.ASCII.GetString(receiveBytes));
				if (receiveBytes.Length < server_list_answer_head.Length) {//+eot.Length
					Masterserver.DebugMessage("Result is too short.");
					break;
				}
				Byte[] start = receiveBytes.Take(server_list_answer_head.Length).ToArray();
				Byte[]  ende = receiveBytes.Skip(server_list_answer_head.Length+1).ToArray();
				Byte[]  tail = receiveBytes.Skip(receiveBytes.Length-eot.Length).ToArray();
				Byte[]  data = receiveBytes.Skip(server_list_answer_head.Length+1).ToArray();
				data = data.Take(data.Length-eot.Length).ToArray();


				if (start.SequenceEqual(server_list_answer_head) && tail.SequenceEqual(eot)) {
					Masterserver.DebugMessage("Answer is valid.");

					string returnData = Encoding.ASCII.GetString(data);
					Masterserver.DebugMessage("Data-String: '" + returnData + "'");
					string[] adressen = returnData.Split('\\');
					if (Masterserver.GetDebug()) {
						foreach (string adresse in adressen) {
							Masterserver.DebugMessage(adresse + "was received");
						}
					}
					if (ende.SequenceEqual(eot)) {
						Masterserver.DebugMessage("But no servers were sent back.");
					} else {
						Masterserver.DebugMessage("The following servers were returned:");
						foreach (string adresse in adressen)
						{
							ServerEntry newcomer = new ServerEntry(adresse);
							ServerList.AddServer(newcomer);
						}
					}
					if (server_list_request00 == server_list_request && returnData  != "") { //This works only if the other side sends all servers know (incl. other versions) when version number is zero. In such cases other queries are not required.
						Masterserver.DebugMessage("Got data and are in first round -> no further queries!");
						return;
					}
				} else {
				if (Masterserver.GetDebug()) {
					Console.WriteLine("start:");
					Parser.DumpBytes(start);
					Console.WriteLine("server_list_answer_head:");
					Parser.DumpBytes(server_list_answer_head);
					Console.WriteLine("ende:");
					Parser.DumpBytes(ende);
					Console.WriteLine("tail:");
					Parser.DumpBytes(tail);
					Console.WriteLine("eot:");
					Parser.DumpBytes(eot);
					Console.WriteLine("data:");
					Parser.DumpBytes(data);
				}
					Masterserver.DebugMessage("Got jibberish here:");
					if (Masterserver.GetDebug()) {
						Parser.DumpBytes(receiveBytes);
					}
				}
			}
        }
		Masterserver.DebugMessage("End of query loop.");
    }

    public static void QueryOtherMasters(string[] masterServerArray) {
        Masterserver.DebugMessage("Querying provided master servers...");
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
        Masterserver.DebugMessage("Querying provided master servers in intervals of " + interval + "s.");
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
        Masterserver.DebugMessage("Starting cleanup thread.");
        Thread thread = new Thread(() => ServerList.CleanupThread());
        thread.Start();
		return thread;
    }

    private static void CleanupThread() {
        while (true) {
            ServerList.Cleanup();
            System.Threading.Thread.Sleep(600000); //Once every ten minutes should suffice
        }
    }
}
