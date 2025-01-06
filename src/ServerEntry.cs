using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class ServerEntry : IEquatable<ServerEntry> {
    IPEndPoint host = null;
    ushort port = 0;
    IPAddress address = null;
    /* This means, the server did not provide a proper procotol (yet).
       Meaning, it is not valid: */
    int protocol = -1;
    bool full = false; // Are all player slots in use?
    bool empty = false; // Are no player slots in use?
    /* This is _NOT_ the network host name, but the name the game server admin
       gave this server (sv_hostname): */
    string hostname = "";
    /* Whenever we update the servers details, this will be set anew. Helps
       deriving, how long ago it was queried. */
    long lastTimeHeardOf = 0;

    private Dictionary <string,string> queryValues = new Dictionary <string,string>();
    private List<Player> playerList = new List<Player>();

    public ServerEntry(IPAddress ip, ushort port) {
        Printer.DebugMessage("Creating ServerEntry from IPAddress and port"
                             + " number.");
        this.port = port;
        this.address = ip;
        this.host = new IPEndPoint(ip, port);
    }

    public ServerEntry(string ipString, ushort port) {
        Printer.DebugMessage("Creating ServerEntry from IP String and port"
                             + " number.");
        this.port = port;
        IPAddress ip = IPAddress.Parse(ipString);
        this.address = ip;
        this.host = new IPEndPoint(ip, port);
    }

    public ServerEntry(Byte[] ipAndPort) {
        Printer.DebugMessage("Entering ServerEntry("
                             + Encoding.ASCII.GetString(ipAndPort) + ")");
        Printer.DebugMessage("Creating ServerEntry from Bytes");
        /* IPv4:  4 bytes host und 2 bytes port =  6 bytes,
                  each encoded as 2 bytes hex   = 12 bytes hex digits
           IPv6: 16 bytes host and 2 bytes port = 18 bytes binary ASCII */
        bool hasAllowedLength = (   ipAndPort.Length == 12
                                 || ipAndPort.Length == 18);
        if (!hasAllowedLength) { // Neither IPv6 nor IPv4
            Printer.DebugMessage("Warning: Address string has no allowed length!");
        }
        else {
            if (ipAndPort.Length == 12) { // IPv4 case
                Printer.DebugMessage("Looks like IPv4.");
                string ip = Parser.getEFIpPortString(ipAndPort);
                byte[] portbytes = QueryStrings.GetSubByteArray(ipAndPort,
                                                                8,
                                                                4);
                ushort port = (ushort) Parser.HexToDec(
                    Encoding.ASCII.GetString(portbytes));
                this.port = port;
                this.address = IPAddress.Parse(ip);
                if (ip.Equals("0.0.0.0")) {
                    /* This should only happen, when HexToDec returns this,
                       which means, malformed server record. */
                    Invalidate();
                }
            }
            else {
                Printer.DebugMessage("Looks like IPv6.");
                // Leaves only IPv6 case
                Byte[] ipBytes   = ipAndPort.Take(16).ToArray();
                Byte[] portBytes = ipAndPort.Skip(16).ToArray();
                ushort port = byteToUshort(portBytes);
                IPAddress address = new IPAddress(ipBytes);
                this.port = port;
                this.address = address;
            }
            if (Printer.GetDebug()) {
                string displayAddress = this.address.ToString();
                if (ipAndPort.Length == 18) {
                    displayAddress = "[" + displayAddress + "]";
                }
                Printer.DebugMessage("Derived IP and Port: "
                                     + displayAddress + ":" + this.port);
            }
        }
        if (!IsValid()) {
            Printer.DebugMessage("Warning: new ServerEntry still"
                                 + " uninitialized!");
        }
        this.host = new IPEndPoint(this.address, this.port);
        Printer.DebugMessage("Derived IPEndPoint: " + this.host);
        Printer.DebugMessage("Leaving ServerEntry("
                             + Encoding.ASCII.GetString(ipAndPort) + ")");
    }

    public ServerEntry(string ipAndPort):base() {
        Printer.DebugMessage("Entering ServerEntry(" + ipAndPort + ")");
        /* IPv4:  4 bytes host und 2 bytes port =  6 bytes,
                 each encoded as 2 bytes hex    = 12 bytes hex digits
           IPv6: 16 Bytes Host und 2 Bytes port = 18 bytes binary ASCII */
        bool hasAllowedLength = (   ipAndPort.Length == 12
                                 || ipAndPort.Length == 18);
        if (!hasAllowedLength) { // Neither IPv6 nor IPv4
            Printer.DebugMessage("Warning: Address string has no allowed length!");
        }
        else {
            if (ipAndPort.Length == 12) { // IPv4 case
                string ipAndPortPattern = "[\\d,A-F,a-f]{12}";
                Regex checker = new Regex (ipAndPortPattern);
                if (checker.IsMatch(ipAndPort)) {
                    string ip = Parser.getEFIpPortString(ipAndPort);
                    ushort port = (ushort) Parser.HexToDec(
                        ipAndPort.Substring(8,4));
                    this.port = port;
                    this.address = IPAddress.Parse(ip);
                    if (this.address.Equals("0.0.0.0")) {
                        Invalidate();
                    }
                }
                else {
                    Printer.DebugMessage("Warning: Faulty IPv4 hex address"
                                         + " provided");
                }
            }
            else {
                // Leaves only IPv6 case
                string ipPart    = ipAndPort.Substring( 0, 16);
                string portPart  = ipAndPort.Substring(16,  2);
                byte[] ipBytes   = Encoding.ASCII.GetBytes(ipPart);
                byte[] portBytes = Encoding.ASCII.GetBytes(portPart);
                ushort port = byteToUshort(portBytes);
                IPAddress address = new IPAddress(ipBytes);
                this.port = port;
                this.address = address;
            }
        }
        if (!IsValid()) {
            Printer.DebugMessage("Warning: new ServerEntry still"
                                 + " uninitialized!");
        }
        this.host = new IPEndPoint(this.address, this.port);
        Printer.DebugMessage("Leaving ServerEntry(" + ipAndPort + ")");
    }

    private void Invalidate() {
        this.lastTimeHeardOf = 0;
        this.protocol = -1;
        // Freeing up some memory
        queryValues = new Dictionary <string,string>();
    }

    public void InvalidateIfTooOld() {
        if (isTooOld()) {
            Printer.DebugMessage(this.ToString()
                                 + " has gotten too old, invalidating it...");
            Invalidate();
        }
        else {
            Printer.DebugMessage(this.ToString()
                                 + " is not too old, yet. Keeping its"
                                 + " validation state.");
        }
    }

    public bool IsValid() {
        return !(   this.port==0
                 || this.lastTimeHeardOf == 0
                 || this.protocol == -1);
    }

    public bool ReadyToQuery() {
        return !(port == 0 || address == null);
    }

    public void SetTimeStamp() {
        this.lastTimeHeardOf = DateTime.UtcNow.Ticks;
        Printer.DebugMessage("SetTimeStamp(), set time stamp to "
                             + this.lastTimeHeardOf);
    }

    public long GetTimeStamp() {
        return this.lastTimeHeardOf;
    }

    private bool isTooOld() {
        long timeNow = DateTime.UtcNow.Ticks;
        long difference = timeNow - this.lastTimeHeardOf;
        // 10 Million ticks per second, 5 minutes + 1 grace second
        return (difference > 301*10E6);
    }

    public bool IsFull() {
        return full;
    }

    public bool IsEmpty() {
        return empty;
    }

    public bool IsIpV4() {
        return NetworkBasics.IsIPv4Address(GetAddress());
    }

    public bool IsIpV6() {
        return NetworkBasics.IsIPv6Address(GetAddress());
    }

    public IPAddress GetAddress() {
        return this.address;
    }

    public string GetAddressString() {
        return this.address.ToString();
    }

    public ushort GetPort() {
        return this.port;
    }

    public IPEndPoint GetEndPoint() {
        return this.host;
    }

    private string portinHex() {
        string portInHex = "0";
        try {
            portInHex = string.Format("{0:x2}", this.port);
        }
        catch (Exception e) {
            Printer.VerboseMessage(e.ToString());
        }
        return portInHex;
    }

    private byte[] portinBin() {
        uint portHigher = (uint)(this.port / 256);
        uint portLower = port - portHigher * 256;
        byte[] portBin = new byte[] {(byte)portHigher, (byte)portLower};
        return portBin;
    }

    private void SetProtocol(string protocol) {
        Printer.DebugMessage("SetProtocol(string protocol)");
        if (!Int32.TryParse(protocol, out this.protocol)) {
            Invalidate();
        }
        Printer.DebugMessage("Set protocol to: " + this.protocol);
    }

    public int GetProtocol() {
        return this.protocol;
    }

    public string GetHostname() {
        return this.hostname;
    }

    private string IPinHex() {
        string addressString = "";
        foreach (byte addressByte in this.address.GetAddressBytes()) {
            addressString += string.Format("{0:x2}", (int)addressByte);
        }
        return addressString;
    }

    private byte[] IPinBin() {
        byte[] addressbytes = this.address.GetAddressBytes();
        return addressbytes;
    }

    public string serverEntryinHex() {
        string serverString = this.IPinHex();
        serverString += this.portinHex();
        return serverString;
    }

    private byte[] serverEntryinBytes() {
        if (!IsValid()) {
            return null;
        }
        byte[] binIP = this.IPinBin();
        byte[] binPort = this.portinBin();
        byte[] serverEntryBin = Parser.ConcatByteArray(new byte[][] {binIP,
                                                                     binPort});
        return serverEntryBin;
    }

    public bool Equals(ServerEntry comparison) {
        return comparison.GetEndPoint().Equals(this.GetEndPoint());
    }

    public override string ToString() {
        return this.serverEntryinHex();
    }

    public Byte[] ToByteArray() {
        if (!IsValid()) {
            return null;
        }
        return this.serverEntryinBytes();
    }

    public Dictionary <string,string> GetData() {
        return this.queryValues;
    }

    public string GetIpRepresentation() {
        string ipAddress = this.address.ToString();
        if (NetworkBasics.IsIPv6Address(this.address)) {
            ipAddress = "[" + ipAddress + "]";
        }
        return ipAddress + ":" + port;
    }

    private byte[] QueryGameServerForInfo() {
        IPAddress destinationIp = this.address;
        int destinationPort = (int)this.port;
        byte[] serverStatusQueryHead = QueryStrings.GetByteArray(
            QueryStrings.stringType.server_status_query_head);

        byte[] receivedBytes = NetworkBasics.GetAnswer(destinationIp,
                                                       destinationPort,
                                                       serverStatusQueryHead);
        return receivedBytes;
    }

    private bool SetProtocolFromQueryResults(Dictionary <string,string> receivedQueryValues) {
        string protocol;
        if (!receivedQueryValues.TryGetValue("protocol", out protocol)) {
            Printer.DebugMessage("Didn't receive any protocol from "
                                 + this.ToString() + ", invalidating it.");
            return false;
        }
        else {
            Printer.DebugMessage("Protocol " + protocol + " received from "
                                 + this.ToString() + ".");
            SetProtocol(protocol);
            return true;
        }
    }

    private void SetHostNameFromQueryResults(Dictionary <string,string> receivedQueryValues) {
        string hostname;
        if (receivedQueryValues.TryGetValue("hostname", out hostname)) {
            Printer.DebugMessage("Got host name '" + hostname
                                 + "' from server.");
            this.hostname=hostname;
        }
    }

    private bool QueryResultsAreValid(byte[] receivedBytes) {
        if (receivedBytes == null) {
            Printer.DebugMessage("Didn't receive any data from "
                                 + this.ToString() + ".");
            return false;
        }
        int payloadStartIndex = QueryStrings.GetByteArray(
            QueryStrings.stringType.server_status_answer_head).Length + 1;
        if (receivedBytes.Length < payloadStartIndex) {
            Printer.DebugMessage("Didn't receive any valid data from " +
                                 this.ToString() + ".");
            return false;
        }
        byte[] serverStatusResponseHeader =
            QueryStrings.GetByteArray(
                QueryStrings.stringType.server_status_answer_head);
        int responseHeaderLength = QueryStrings.GetByteArray(
            QueryStrings.stringType.server_status_answer_head).Length;
        Byte[] responseHead =
            receivedBytes.Take(responseHeaderLength).ToArray();
        return responseHead.SequenceEqual(serverStatusResponseHeader);
    }

    public void QueryInfo() {
        Printer.DebugMessage("Querying status from server " + this + "...");
        if (!ReadyToQuery()) {
            Printer.DebugMessage("Server hasn't been initalized, yet.");
            return;
        }
        byte[] receivedBytes = QueryGameServerForInfo();

        Printer.DebugMessage("Received data from " + this.ToString() + ".");

        if (QueryResultsAreValid(receivedBytes)) {
            int responseHeaderLength =
                QueryStrings.GetByteArray(
                    QueryStrings.stringType.server_status_answer_head).Length;
            int payloadStartIndex = responseHeaderLength + 1;
            Byte[] receivedPayload =
                receivedBytes.Skip(payloadStartIndex).ToArray();
            Printer.DebugMessage("Found an expected header from "
                                 + this.ToString() + ".");
            string returnData = Encoding.ASCII.GetString(receivedPayload);
            string[] blocks = returnData.Split('"');
            if (blocks.Length != 2) {
                Printer.DebugMessage("Warning: Received uneven number of data"
                                     + " values from remote host.");
                Invalidate();
                return;
            }
            string datablock = blocks[0].Substring(1);
            Printer.DebugMessage("Received data '" + datablock + "' from "
                                 + this.ToString());

            Dictionary <string,string> receivedQueryValues = Parser.SplitStringToParameters(datablock);
            if (receivedQueryValues == null) {
                Printer.DebugMessage("receivedQueryValues are null, assuming"
                                     + " server went offline and deleting its"
                                     + " data");
                Invalidate();
                return;
            }

            if (!SetProtocolFromQueryResults(receivedQueryValues)) {
                Invalidate();
                return;
            }
            SetHostNameFromQueryResults(receivedQueryValues);

            this.queryValues = Parser.ConcatDictionaries(this.queryValues,
                                                         receivedQueryValues);
            string sv_maxclients;
            string clients;
            bool noSv_maxclients = !queryValues.TryGetValue("sv_maxclients",
                                                            out sv_maxclients);
            bool noClientData = !queryValues.TryGetValue("clients",
                                                         out clients);
            bool iDunno = (   noSv_maxclients
                           || noClientData);
            if (iDunno) {
                Printer.DebugMessage("Lacking some information, assuming server"
                                     + " is neither full nor empty.");
                this.full = false;
                this.empty = false;
            }
            else {
                int noOfClients = int.Parse(clients);
                int maxNoOfClients = int.Parse(sv_maxclients);
                if (noOfClients == 0) {
                    Printer.DebugMessage("Found Server to be empty.");
                    this.empty = true;
                }
                else {
                    this.empty = false;
                }
                if (noOfClients.Equals(maxNoOfClients)) {
                    Printer.DebugMessage("Found Server to be full.");
                    this.full = true;
                }
                else {
                    this.full = false;
                }
            }
            SetTimeStamp();
            return;
        }
        else {
            if (receivedBytes == null) {
                Printer.DebugMessage("Didn't receive any response at all.");
            }
            else {
                Printer.DebugMessage("Unrecognized response '"
                                     + Encoding.ASCII.GetString(receivedBytes)
                                     + "' from " + this.ToString());
            }
            Invalidate();
            return;
        }

    }

    public void QueryDetails() {
        Printer.DebugMessage("Querying details from server " + this + "...");
        if (!IsValid()) {
            Printer.DebugMessage("This server has not been initalized, yet.");
            return;
        }

        IPAddress destinationIp = this.address;
        int destinationPort = (int)this.port;
        byte[] serverDetailsQueryHead  = QueryStrings.GetByteArray(
            QueryStrings.stringType.server_details_query_head);
        byte[] serverDetailsAnswerHead = QueryStrings.GetByteArray(
            QueryStrings.stringType.server_details_answer_head);

        byte[] receivedBytes = NetworkBasics.GetAnswer(destinationIp,
                                                       destinationPort,
                                                       serverDetailsQueryHead);
        if (receivedBytes == null) {
            Printer.DebugMessage("Didn't receive any data from "
                                 + this.ToString() + ".");
            return;
        }

        Byte[] start    = receivedBytes.Take(
            Encoding.ASCII.GetString(serverDetailsAnswerHead).Length).ToArray();
        Byte[] payload  = receivedBytes.Skip(
            Encoding.ASCII.GetString(serverDetailsAnswerHead).Length).ToArray();

        Printer.DebugMessage("Received data from " + this.ToString() + ".");
        if (start.SequenceEqual(serverDetailsAnswerHead)) {
            Printer.DebugMessage("Found an expected header from "
                                 + this.ToString() + ".");
            string returnData = Encoding.ASCII.GetString(payload);
            string serverDetails = Parser.GetDataFromDetails(returnData);
            this.playerList = Parser.GetPlayersFromDetails(returnData);
            Dictionary <string,string> detailValues =
                Parser.SplitStringToParameters(serverDetails);
            if (detailValues == null) {
                Printer.DebugMessage("I got nothing.");
                return;
            }

            this.queryValues = Parser.ConcatDictionaries(this.queryValues,
                                                         detailValues);
            return;
        }
        else {
            Printer.DebugMessage("Unrecognized response '"
                                 + Encoding.ASCII.GetString(receivedBytes)
                                 + "' from " + this.ToString());
            Invalidate();
            return;
        }

    }

    // Currently unused:
    public List<Player> GetPlayers() {
        return this.playerList;
    }

    public static ushort byteToUshort(byte[] bytes) {
        if (bytes == null || bytes.Length != 2) {
            return 0;
        }
        return (ushort) (bytes[0] * 256 + bytes[1]);
    }
}
