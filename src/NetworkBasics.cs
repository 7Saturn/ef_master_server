using System.Net.Sockets;
using System.Net;
using System;
using System.Collections.Generic;

public static class NetworkBasics {

	public readonly static int timeoutms = 500;

	public static UdpClient NewLocalClient (int start_port = 27960, int end_port = 65535) {
		UdpClient udpClient = null;
        while (start_port <= end_port && udpClient == null) {
			try {
				udpClient = new UdpClient(start_port);
			}
			catch (SocketException e) {
				if (e.ErrorCode == 10048) {//Port is already in use
					start_port++;
				} else {
					throw new CannotOpenUDPPortException();
				}
			}
		}
		if (udpClient == null) {
			throw new CannotOpenUDPPortException();
		}
		return udpClient;
	}

    public static byte[] GetAnswer(IPAddress destination_ip, int destination_port, byte[] sendBytes) {
        UdpClient udpClient = null;
        try{
            udpClient = NetworkBasics.NewLocalClient();
        }
        catch (Exception e) {
            Console.WriteLine("Could not get an open connection to destination game server {0}:{1}.", destination_ip, destination_port);
			Console.WriteLine(e.Message);
            return null;
        }
		try{
			udpClient.Connect(destination_ip, destination_port);
			Printer.DebugMessage("Got connected to " + destination_ip + ":" + destination_port);
			Printer.DebugMessage("Sending...");
            udpClient.Send(sendBytes, sendBytes.Length);

			Printer.DebugMessage("Waiting for response from " + destination_ip + ":" + destination_port + " for " + NetworkBasics.timeoutms + "ms...");
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(destination_ip, destination_port);
            Printer.DebugMessage("Endpoint active...");
            // Won't block the entire program when receiving nothing, but requires a reasonable timeout value
			var asyncResult = udpClient.BeginReceive(null, null);
            Printer.DebugMessage("Beginning receiving.");
			asyncResult.AsyncWaitHandle.WaitOne(NetworkBasics.timeoutms);
            Printer.DebugMessage("Handle active...");
			byte[] receiveBytes = null;
			if (asyncResult.IsCompleted)
			{
				try
				{
					receiveBytes = udpClient.EndReceive(asyncResult, ref RemoteIpEndPoint);
                    Printer.DebugMessage("Received!");
				}
				catch (Exception ex)
				{
                    Printer.DebugMessage("catching " + ex.Message + " for " + destination_ip + ":" + destination_port);
					udpClient.Close();
					return null;
				}
			}
			udpClient.Close();
			if (receiveBytes == null) {
				Printer.DebugMessage("Nothing ever came from " + destination_ip + ":" + destination_port + ".");
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
            Console.WriteLine("Could not get data from destination host {0}:{1}.", destination_ip, destination_port);
			Console.WriteLine(e.Message);
            return null;
        }
    }

    public static IPAddress resolve_host(string master_host) {
        Printer.DebugMessage("resolve_host('" + master_host + "')");
        IPAddress address;
        try {
            address = IPAddress.Parse(master_host);
            Printer.DebugMessage("'" + master_host + "' is already an IP address, no resolution required.");
            return address;
        }
        catch (Exception e) {
            Printer.DebugMessage(e.ToString());
            Printer.DebugMessage(master_host + " is not a valid IP address. Trying to resolve it, assuming it is a host name...");
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
                    Printer.DebugMessage("Could not resolve " + master_host + " to a valid IP address.");
                    return null;
                }
                Printer.DebugMessage("Resolved " + master_host + " to '" + address.ToString() + "'.");
                return address;
            }
            catch (Exception e2) {
                Printer.DebugMessage(e2.ToString());
                Printer.DebugMessage("Could not resolve " + master_host + " to a valid IP address.");
                return null;
            }
        }
    }
}
