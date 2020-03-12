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
    int protocol = 0;
    bool full = false;
    bool empty = false;
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
        Masterserver.DebugMessage("ServerEntry("+ip_port+")");
        string ip_port_pattern = "[\\d,A-F,a-f]{12}";
        Regex checker = new Regex (ip_port_pattern);
        if (checker.IsMatch(ip_port)) {
            int ip1 = Parser.HexToDec(ip_port.Substring(0,2));
            int ip2 = Parser.HexToDec(ip_port.Substring(2,2));
            int ip3 = Parser.HexToDec(ip_port.Substring(4,2));
            int ip4 = Parser.HexToDec(ip_port.Substring(6,2));
            string ip = ip1+"."+ip2+"."+ip3+"."+ip4;
            ushort port = (ushort) Parser.HexToDec(ip_port.Substring(8,4));
            this.port = port;
            this.address = IPAddress.Parse(ip);
        }
        else {
            Masterserver.DebugMessage("Warning: No valid address string provided!");
        }
        Masterserver.DebugMessage("/ServerEntry(string)");
    }

    public bool IsFull() {
        return full;
    }

    public bool IsEmpty() {
        return empty;
    }

    private string port_in_hex() {
        return string.Format("{0:x2}", this.port);
    }

    private void SetProtocol(string protocol) {
        Masterserver.DebugMessage("SetProtocol(string protocol)");
        if (!Int32.TryParse(protocol, out this.protocol)) {
            this.protocol = 0;
        }
        Masterserver.DebugMessage("Set protocol to: "+this.protocol);
    }

    private void SetProtocol(int protocol) {
        Masterserver.DebugMessage("SetProtocol(int protocol)");
        this.protocol = protocol;
        Masterserver.DebugMessage("Set protocol to: "+this.protocol);
    }

    public int GetProtocol() {
        return this.protocol;
    }

    private string ip_in_hex() {
        byte[] addressbytes = this.address.GetAddressBytes();
        string first = string.Format("{0:x2}", (int)addressbytes[0]);
        string second = string.Format("{0:x2}", (int)addressbytes[1]);
        string third = string.Format("{0:x2}", (int)addressbytes[2]);
        string fourth = string.Format("{0:x2}", (int)addressbytes[3]);
        return first+second+third+fourth;
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

    public bool Equals(ServerEntry vergleichswert) {
        string selbst_hex_wert = this.ToString();
        string vergleichs_hex_wert = vergleichswert.ToString();
        return vergleichs_hex_wert.Equals(selbst_hex_wert);
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
        return ip_address+":"+port;
    }

    public void QueryInfo() {
        //Der ganze Block sollte so umgeschrieben werden, dass für die Queries nur noch die zurückgegebenen Datenblöcke mit den eigentlichen Wertepaaren ausgewertet werden müssen.
        //Was noch fehlt, ist die Detail-Abfrage, die nicht nur die Standard-Infos abholt
        Masterserver.DebugMessage("Querying status from server "+this+"...");
        if (this.port == 0) {
            Masterserver.DebugMessage("Server hasn't been initalized.");
            return;
        }

        IPAddress destination_ip = this.address;
        int destination_port = (int)this.port;
        byte[] server_status_query_head = QueryStrings.GetArray("server_status_query_head");
        byte[] server_response_head = QueryStrings.GetArray("server_status_answer_head");

        byte[] receivedBytes = NetworkBasics.GetAnswer(destination_ip, destination_port, server_status_query_head);
        if (receivedBytes == null) {
            Masterserver.DebugMessage("Didn't receive any data from "+this.ToString()+".");
            return;
        }

        Byte[] start = receivedBytes.Take(Encoding.ASCII.GetString(server_response_head).Length).ToArray();
        Byte[] ende  = receivedBytes.Skip(Encoding.ASCII.GetString(server_response_head).Length+1).ToArray();

        Masterserver.DebugMessage("Received data from "+this.ToString()+".");
        if (start.SequenceEqual(server_response_head)) {
            Masterserver.DebugMessage("Found an expected header from "+this.ToString()+".");
            string returnData = Encoding.ASCII.GetString(ende);
            string[] bloecke = returnData.Split('"');
            if (bloecke.Length != 2) {
                Masterserver.DebugMessage("Warning: Received uneven number of data values from remote host.");
                this.protocol = 0;
                return;
            }
            string datenblock = bloecke[0].Substring(1);
            Masterserver.DebugMessage("Received data '"+datenblock+"' from "+this.ToString());

            Dictionary <string,string> temp_query_values = Parser.SplitStringToParameters(datenblock);
            if (temp_query_values == null) {
                this.protocol = 0;
                query_values = new Dictionary <string,string>();
                return;
            }

            string protocol;
            if (!temp_query_values.TryGetValue("protocol", out protocol)) {
                Masterserver.DebugMessage("Didn't receive any protocol from "+this.ToString()+".");
                SetProtocol("0");
            } else {
                Masterserver.DebugMessage("Protocol "+protocol+" received from "+this.ToString()+".");
                SetProtocol(protocol);
            }
            string sv_maxclients;
            string clients;
            if (!query_values.TryGetValue("sv_maxclients", out sv_maxclients)) {
                this.full = false;
                this.empty = false;
            }
            else if (!query_values.TryGetValue("clients", out clients)) {
                this.full = false;
                this.empty = false;
            }
            else {
                int clients_n = int.Parse(clients);
                int sv_maxclients_n = int.Parse(clients);
                if (clients_n == 0) {
                    this.empty = true;
                }
                if (clients_n.Equals(sv_maxclients_n)) {
                    this.full = true;
                }
            }
            this.query_values = Parser.ConcatDictonaries(this.query_values,temp_query_values);
            return;
        }
        else {
            Masterserver.DebugMessage("Unrecognized response '"+Encoding.ASCII.GetString(receivedBytes)+"' from "+this.ToString());
            this.protocol = 0;
            return;
        }

    }

    public void QueryDetails() {
        Masterserver.DebugMessage("Querying details from server "+this+"...");
        if (this.protocol == 0) {
            Masterserver.DebugMessage("This server has not been initalized, yet.");
            return;
        }

        IPAddress destination_ip = this.address;
        int destination_port = (int)this.port;
        byte[] server_details_query_head = QueryStrings.GetArray("server_details_query_head");
        byte[] server_details_answer_head = QueryStrings.GetArray("server_details_answer_head");

        byte[] receivedBytes = NetworkBasics.GetAnswer(destination_ip, destination_port, server_details_query_head);
        if (receivedBytes == null) {
            Masterserver.DebugMessage("Didn't receive any data from "+this.ToString()+".");
            return;
        }

        Byte[] start = receivedBytes.Take(Encoding.ASCII.GetString(server_details_answer_head).Length).ToArray();
        Byte[] payload  = receivedBytes.Skip(Encoding.ASCII.GetString(server_details_answer_head).Length).ToArray();

        Masterserver.DebugMessage("Received data from "+this.ToString()+".");
        if (start.SequenceEqual(server_details_answer_head)) {
            Masterserver.DebugMessage("Found an expected header from "+this.ToString()+".");
            string returnData = Encoding.ASCII.GetString(payload);
            string serverDetails = Parser.GetDataFromDetails(returnData);
            this.playerList = Parser.GetPlayersFromDetails(returnData);
            Dictionary <string,string> detail_values = Parser.SplitStringToParameters(serverDetails);
            if (detail_values == null) {
                Masterserver.DebugMessage("Nix bekommen");
                return;
            }

            this.query_values = Parser.ConcatDictonaries(this.query_values, detail_values);
            return;
        }
        else {
            Masterserver.DebugMessage("Unrecognized response '"+Encoding.ASCII.GetString(receivedBytes)+"' from "+this.ToString());
            this.protocol = 0;
            return;
        }

    }

    public List<Player> GetPlayers() {
        return this.playerList;
    }
}
