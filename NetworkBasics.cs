using System.Net.Sockets;
using System.Net;
using System;
public static class NetworkBasics {

	public readonly static int timeoutms = 500;

	public static UdpClient NewLocalClient (int start_port = 27960, int end_port = 65535) {
		UdpClient udpClient = null;
        while (start_port <= end_port && udpClient == null) {
			try {
				udpClient = new UdpClient(start_port);
			}
			catch (SocketException e) {
				if (e.ErrorCode == 10048) {
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
			Masterserver.DebugMessage("Got Connect to "+destination_ip+":"+destination_port);
			Masterserver.DebugMessage("Sending...");
            udpClient.Send(sendBytes, sendBytes.Length);

			Masterserver.DebugMessage("Waiting for response from "+destination_ip+":"+destination_port+" for "+NetworkBasics.timeoutms+"ms...");
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(destination_ip, destination_port);
            Masterserver.DebugMessage("Endpoint active...");
            // Won't block the entire program when receiving nothing, but requires a reasonable timeout value
			var asyncResult = udpClient.BeginReceive( null, null );
            Masterserver.DebugMessage("Beginning receiving.");
			asyncResult.AsyncWaitHandle.WaitOne(NetworkBasics.timeoutms);
            Masterserver.DebugMessage("Handle active...");
			byte[] receiveBytes = null;
			if (asyncResult.IsCompleted)
			{
				try
				{
					receiveBytes = udpClient.EndReceive(asyncResult, ref RemoteIpEndPoint);
                    Masterserver.DebugMessage("Received!");
				}
				catch (Exception ex)
				{
                    Masterserver.DebugMessage("catching " + ex.Message + " for " + destination_ip + ":" + destination_port);
					udpClient.Close();
					return null;
				}
			}
			if (receiveBytes == null) {
				Masterserver.DebugMessage("Nothing ever came from "+destination_ip+":"+destination_port+".");
				udpClient.Close();
				return null;
			}
			udpClient.Close();
            while (receiveBytes.Length > 0 && receiveBytes[receiveBytes.Length-1] == 0) {
                Masterserver.DebugMessage("Trimming tailing zero byte.");
                Array.Resize(ref receiveBytes,receiveBytes.Length -1);
            }
            return receiveBytes;
        }
        catch (Exception e) {
            Console.WriteLine("Could not get data from destination host {0}:{1}.", destination_ip, destination_port);
			Console.WriteLine(e.Message);
            return null;
        }
    }
}
