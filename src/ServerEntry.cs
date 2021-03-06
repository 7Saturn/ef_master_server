using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;

public class ServerEntry : IEquatable<ServerEntry>{
    ushort port;
    IPAddress address;
    int protocol = -1;
    bool full = false;
    bool empty = false;
    string hostname = "";

    private Dictionary <string,string> query_values = new Dictionary <string,string>();
    private List<Player> playerList = new List<Player>();

    public ServerEntry() {
        this.port = 0;
        this.address = null;
    }

    public ServerEntry(IPAddress ip, ushort port) {
        this.port = port;
        this.address = ip;
    }

    public ServerEntry(string ip, ushort port) {
        this.port = port;
        this.address = IPAddress.Parse(ip);
    }

    public ServerEntry(string ip_port):base() {
        Printer.DebugMessage("Entering ServerEntry(" + ip_port + ")");
        string ip_port_pattern = "[\\d,A-F,a-f]{12}";
        Regex checker = new Regex (ip_port_pattern);
        if (checker.IsMatch(ip_port)) {
            int ip1 = Parser.HexToDec(ip_port.Substring(0,2));
            int ip2 = Parser.HexToDec(ip_port.Substring(2,2));
            int ip3 = Parser.HexToDec(ip_port.Substring(4,2));
            int ip4 = Parser.HexToDec(ip_port.Substring(6,2));
            string ip = ip1 + "." + ip2 + "." + ip3 + "." + ip4;
            ushort port = (ushort) Parser.HexToDec(ip_port.Substring(8,4));
            this.port = port;
            this.address = IPAddress.Parse(ip);
        }
        else {
            Printer.DebugMessage("Warning: No valid address string provided!");
        }
        Printer.DebugMessage("Leaving ServerEntry(" + ip_port + ")");
    }

    public bool IsFull() {
        return full;
    }

    public bool IsEmpty() {
        return empty;
    }

    public string GetAddress() {
        return this.address.ToString();
    }

    public ushort GetPort() {
        return this.port;
    }

    private string port_in_hex() {
        return string.Format("{0:x2}", this.port);
    }

    private void SetProtocol(string protocol) {
        Printer.DebugMessage("SetProtocol(string protocol)");
        if (!Int32.TryParse(protocol, out this.protocol)) {
            this.protocol = -1;
        }
        Printer.DebugMessage("Set protocol to: " + this.protocol);
    }

    private void SetProtocol(int protocol) {
        Printer.DebugMessage("SetProtocol(int protocol)");
        this.protocol = protocol;
        Printer.DebugMessage("Set protocol to: " + this.protocol);
    }

    public int GetProtocol() {
        return this.protocol;
    }

    public string GetHostname() {
        return this.hostname;
    }

    private string ip_in_hex() {
        byte[] addressbytes = this.address.GetAddressBytes();
        string first = string.Format("{0:x2}", (int)addressbytes[0]);
        string second = string.Format("{0:x2}", (int)addressbytes[1]);
        string third = string.Format("{0:x2}", (int)addressbytes[2]);
        string fourth = string.Format("{0:x2}", (int)addressbytes[3]);
        return first + second + third + fourth;
    }

    private string server_entry_in_hex() {
        if (this.port == 0) {
            return null;
        }
        string server_string = "";
        server_string += this.ip_in_hex();
        server_string += this.port_in_hex();
        return server_string;
    }

    public bool Equals(ServerEntry comparison) {
        string own_hex_value = this.ToString();
        string comparison_hex_value = comparison.ToString();
        return comparison_hex_value.Equals(own_hex_value);
    }

    public override string ToString() {
        if (this.port == 0) {
            return "";
        }
        return this.server_entry_in_hex();
    }

    public Thread QueryInfoThreaded() {
        Thread thread = new Thread(new ThreadStart(this.QueryInfo));
        thread.Start();
        return thread;
    }

    public Dictionary <string,string> GetData() {
        return this.query_values;
    }

    public string GetIpRepresentation () {
        string ip_address = this.address.ToString();
        return ip_address + ":" + port;
    }

    public void QueryInfo() {
        Printer.DebugMessage("Querying status from server " + this + "...");
        if (this.port == 0) {
            Printer.DebugMessage("Server hasn't been initalized.");
            return;
        }

        IPAddress destination_ip = this.address;
        int destination_port = (int)this.port;
        byte[] server_status_query_head = QueryStrings.GetArray("server_status_query_head");
        byte[] server_response_head = QueryStrings.GetArray("server_status_answer_head");

        byte[] receivedBytes = NetworkBasics.GetAnswer(destination_ip, destination_port, server_status_query_head);
        if (receivedBytes == null) {
            Printer.DebugMessage("Didn't receive any data from " + this.ToString() + ".");
            return;
        }

        Byte[] start = receivedBytes.Take(Encoding.ASCII.GetString(server_response_head).Length).ToArray();
        Byte[] end   = receivedBytes.Skip(Encoding.ASCII.GetString(server_response_head).Length + 1).ToArray();

        Printer.DebugMessage("Received data from " + this.ToString() + ".");
        if (start.SequenceEqual(server_response_head)) {
            Printer.DebugMessage("Found an expected header from " + this.ToString() + ".");
            string returnData = Encoding.ASCII.GetString(end);
            string[] blocks = returnData.Split('"');
            if (blocks.Length != 2) {
                Printer.DebugMessage("Warning: Received uneven number of data values from remote host.");
                this.protocol = -1;
                return;
            }
            string datablock = blocks[0].Substring(1);
            Printer.DebugMessage("Received data '" + datablock + "' from " + this.ToString());

            Dictionary <string,string> received_query_values = Parser.SplitStringToParameters(datablock);
            if (received_query_values == null) {
                Printer.DebugMessage("received_query_values are null, assuming server went offline and deleting its data");
                this.protocol = -1;
                this.port = 0;
                query_values = new Dictionary <string,string>();
                return;
            }

            string protocol;
            if (!received_query_values.TryGetValue("protocol", out protocol)) {
                Printer.DebugMessage("Didn't receive any protocol from " + this.ToString() + ".");
                SetProtocol("-1");
            }
            else {
                Printer.DebugMessage("Protocol " + protocol + " received from " + this.ToString() + ".");
                SetProtocol(protocol);
            }

            string hostname;
            if (received_query_values.TryGetValue("hostname", out hostname)) {
                Printer.DebugMessage("Got host name '" + hostname + "' from server.");
                this.hostname=hostname;
            }

            this.query_values = Parser.ConcatDictionaries(this.query_values,received_query_values);
            string sv_maxclients;
            string clients;
            if (   !query_values.TryGetValue("sv_maxclients", out sv_maxclients)
                || !query_values.TryGetValue("clients", out clients)) {
                Printer.DebugMessage("Lacking some information, assuming server is neither full nor empty.");
                this.full = false;
                this.empty = false;
            }
            else {
                int clients_n = int.Parse(clients);
                int sv_maxclients_n = int.Parse(sv_maxclients);
                if (clients_n == 0) {
                    Printer.DebugMessage("Found Server to be empty.");
                    this.empty = true;
                }
                else {
                    this.empty = false;
                }
                if (clients_n.Equals(sv_maxclients_n)) {
                    Printer.DebugMessage("Found Server to be full.");
                    this.full = true;
                }
                else {
                    this.full = false;
                }
            }
            return;
        }
        else {
            Printer.DebugMessage("Unrecognized response '" + Encoding.ASCII.GetString(receivedBytes) + "' from " + this.ToString());
            this.protocol = -1;
            return;
        }

    }

    public void QueryDetails() {
        Printer.DebugMessage("Querying details from server " + this + "...");
        if (this.protocol == -1) {
            Printer.DebugMessage("This server has not been initalized, yet.");
            return;
        }

        IPAddress destination_ip = this.address;
        int destination_port = (int)this.port;
        byte[] server_details_query_head = QueryStrings.GetArray("server_details_query_head");
        byte[] server_details_answer_head = QueryStrings.GetArray("server_details_answer_head");

        byte[] receivedBytes = NetworkBasics.GetAnswer(destination_ip, destination_port, server_details_query_head);
        if (receivedBytes == null) {
            Printer.DebugMessage("Didn't receive any data from " + this.ToString() + ".");
            return;
        }

        Byte[] start    = receivedBytes.Take(Encoding.ASCII.GetString(server_details_answer_head).Length).ToArray();
        Byte[] payload  = receivedBytes.Skip(Encoding.ASCII.GetString(server_details_answer_head).Length).ToArray();

        Printer.DebugMessage("Received data from " + this.ToString() + ".");
        if (start.SequenceEqual(server_details_answer_head)) {
            Printer.DebugMessage("Found an expected header from " + this.ToString() + ".");
            string returnData = Encoding.ASCII.GetString(payload);
            string serverDetails = Parser.GetDataFromDetails(returnData);
            this.playerList = Parser.GetPlayersFromDetails(returnData);
            Dictionary <string,string> detail_values = Parser.SplitStringToParameters(serverDetails);
            if (detail_values == null) {
                Printer.DebugMessage("I got nothing.");
                return;
            }

            this.query_values = Parser.ConcatDictionaries(this.query_values, detail_values);
            return;
        }
        else {
            Printer.DebugMessage("Unrecognized response '" + Encoding.ASCII.GetString(receivedBytes) + "' from " + this.ToString());
            this.protocol = -1;
            return;
        }

    }

    public List<Player> GetPlayers() {
        return this.playerList;
    }
}
