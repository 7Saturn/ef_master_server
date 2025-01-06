using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

public static class NetworkBasics {

    private readonly static int timeoutms = 500;

    public static UdpClient NewLocalServer(IPEndPoint localIPEndpoint) {
        UdpClient listener = null;
        try {
            listener = new UdpClient(localIPEndpoint);
        }
        catch (SocketException e) {
            if (e.ErrorCode == 10048) { // WSAEADDRINUSE
                Console.Error.WriteLine("Could not bind server to IP {0} and"
                                        + " port {1}. Are you sure, this is a"
                                        + " proper IP address of a local"
                                        + " network interface and the port is"
                                        + " still unused?",
                                        localIPEndpoint.Address,
                                        localIPEndpoint.Port);
            }
            else if (e.ErrorCode == 10049) { // WSAEADDRNOTAVAIL
                Console.Error.WriteLine("Could not bind server to IP {0}. Are"
                                        + " you sure, this is a proper IP"
                                        + " address of a local network"
                                        + " interface?",
                                        localIPEndpoint.Address);
            }
            else {
                Console.Error.WriteLine("Something unexpected happend, while"
                                        + " trying to open local IP end point:"
                                        + "\n" + e.ToString());
                Console.Error.WriteLine("Error-Code: " + e.ErrorCode);
            }
            Environment.Exit(1);
        }
        return listener;
    }

    public static UdpClient NewLocalClient(AddressFamily protocolFamily,
                                           int startPort = 27960,
                                           int endPort = 65535) {
        /* Why 27960? That's the standard port EF uses as well,
           for clients and servers. */
        Printer.DebugMessage("Creating NewLocalClient...");
        UdpClient udpClient = null;
        while (startPort <= endPort && udpClient == null) {
            try {
                udpClient = new UdpClient(startPort, protocolFamily);
            }
            catch (SocketException e) {
                if (e.ErrorCode == 10048) {//Port is already in use
                    startPort++;
                }
                else {
                    throw new CannotOpenUDPPortException("Could not open a UDP"
                                                         + " port for protocol "
                                                         + protocolFamily);
                }
            }
        }
        if (udpClient == null) {
            throw new CannotOpenUDPPortException("Could not open a UDP port for"
                                                 + " protocol "
                                                 + protocolFamily);
        }
        Printer.DebugMessage("New local client is listening on port "
                             + startPort);
        return udpClient;
    }

    public static byte[] GetAnswer(IPAddress destinationIp,
                                   int destinationPort,
                                   byte[] sendBytes) {
        string displayIP = destinationIp.ToString();
        if (IsIPv6Address(destinationIp)) {
            displayIP = "[" + displayIP + "]";
        }

        if (IsIPv6Address(destinationIp)) {
            Printer.DebugMessage("Trying to open v6 socket...");
        }
        if (IsIPv4Address(destinationIp)) {
            Printer.DebugMessage("Trying to open v4 socket...");
        }
        UdpClient udpClient = null;
        try {
            udpClient = NetworkBasics.NewLocalClient(
                destinationIp.AddressFamily);
        }
        catch (Exception e) {
            Console.WriteLine("Could not get an open connection to destination"
                              + " game server {0}:{1}.",
                              destinationIp, destinationPort);
            Console.WriteLine(e.Message);
            return null;
        }
        try {
            udpClient.Connect(destinationIp, destinationPort);
            Printer.DebugMessage("Got connected to " + displayIP + ":"
                                 + destinationPort);
            Printer.DebugMessage("Sending...");
            udpClient.Send(sendBytes, sendBytes.Length);

            Printer.DebugMessage("Waiting for response from " + displayIP + ":"
                                 + destinationPort + " for "
                                 + NetworkBasics.timeoutms + "ms...");
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(destinationIp,
                                                         destinationPort);
            Printer.DebugMessage("Endpoint active...");
            /* Won't block the entire program when receiving nothing, but
               requires a reasonable timeout value */
            var asyncResult = udpClient.BeginReceive(null, null);
            Printer.DebugMessage("Beginning receiving.");
            asyncResult.AsyncWaitHandle.WaitOne(NetworkBasics.timeoutms);
            Printer.DebugMessage("Handle active...");
            byte[] receiveBytes = null;
            if (asyncResult.IsCompleted) {
                try {
                    receiveBytes = udpClient.EndReceive(asyncResult,
                                                        ref RemoteIpEndPoint);
                    Printer.DebugMessage("Received!");
                }
                catch (Exception ex) {
                    Printer.DebugMessage("catching " + ex.Message + " for "
                                         + displayIP + ":" + destinationPort);
                    udpClient.Close();
                    return null;
                }
            }
            udpClient.Close();
            if (receiveBytes == null) {
                Printer.DebugMessage("Nothing ever came from " + displayIP + ":"
                                     + destinationPort + ".");
                return null;
            }
            while (   receiveBytes.Length > 0
                   && receiveBytes[receiveBytes.Length-1] == 0) {
                Printer.DebugMessage("Trimming tailing zero byte.");
                Array.Resize(ref receiveBytes, receiveBytes.Length - 1);
            }
            return receiveBytes;
        }
        catch (Exception e) {
            Console.WriteLine("Could not get data from destination host"
                              + " {0}:{1}.",
                              displayIP, destinationPort);
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public static IPAddress[] ResolveHosts(string masterHostName,
                                           AddressFamily protocolFamily) {
        Printer.DebugMessage("resolveHosts('" + masterHostName + ", "
                             + protocolFamily + "')");
        try {
            IPAddress address = IPAddress.Parse(masterHostName);
            Printer.DebugMessage("'" + masterHostName
                                 + "' is already an IP address, no resolving"
                                 + " required.");
            Printer.DebugMessage(address.AddressFamily.ToString());
            Printer.DebugMessage(protocolFamily.ToString());
            if (AddressFamilyFits(address, protocolFamily)) {
                Printer.DebugMessage("Was requested, so let's use it!");
                return new IPAddress[]{address};
            }
            Printer.DebugMessage("Was NOT requested, so returning null.");

            return null;
        }
        catch (Exception e) {
            Printer.DebugMessage(e.ToString());
            Printer.DebugMessage(masterHostName + " is not a valid IP address."
                                 + " Trying to resolve it, assuming it is a"
                                 + " host name...");
            try {
                IPAddress[] ipaddresses =
                    Dns.GetHostEntry(masterHostName).AddressList;
                List<IPAddress> temporaryIpAddresses = new List<IPAddress>();
                foreach (IPAddress ipaddress in ipaddresses) {
                    if (ipaddress.AddressFamily == protocolFamily) {
                        Printer.DebugMessage("Adding following resolved address"
                                             + " to the list: " + ipaddress);
                        temporaryIpAddresses.Add(ipaddress);
                    }
                    else {
                        Printer.DebugMessage("Skipping " + ipaddress);
                    }
                }
                ipaddresses = temporaryIpAddresses.ToArray();
                if (ipaddresses.Length > 0) {
                    Printer.DebugMessage("Resolved " + masterHostName + " to "
                                         + ipaddresses.Length
                                         + " IP addresses of type "
                                         + protocolFamily + ".");
                    return ipaddresses;
                }
                else {
                    Printer.DebugMessage("Could not resolve " + masterHostName
                                         + " to a valid IP address.");
                    return null;
                }
            }
            catch (Exception e2) {
                Printer.DebugMessage(e2.ToString());
                Printer.DebugMessage("Could not resolve " + masterHostName
                                     + " to a valid IP address.");
                return null;
            }
        }
    }

    public static bool IsIPv6Address(IPEndPoint Ip) {
        return Ip.AddressFamily.ToString().Equals(
            ProtocolFamily.InterNetworkV6.ToString());
    }

    public static bool IsIPv4Address(IPEndPoint Ip) {
        return Ip.AddressFamily.ToString().Equals(
            ProtocolFamily.InterNetwork.ToString());
    }

    public static bool IsIPv6Address(IPAddress Ip) {
        return Ip.AddressFamily == AddressFamily.InterNetworkV6;
    }

    public static bool IsIPv4Address(IPAddress Ip) {
        return Ip.AddressFamily == AddressFamily.InterNetwork;
    }

    public static bool IsIPv6Address(AddressFamily ipProtocol) {
        return ipProtocol == AddressFamily.InterNetworkV6;
    }

    public static bool IsIPv4Address(AddressFamily ipProtocol) {
        return ipProtocol == AddressFamily.InterNetwork;
    }

    public static bool AddressFamilyFits(IPAddress address,
                                         AddressFamily protocolFamily) {
        return address.AddressFamily.ToString().Equals(
            protocolFamily.ToString());
    }

}
