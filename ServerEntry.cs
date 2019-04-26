using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class ServerEntry : IEquatable<ServerEntry>{
	ushort port;
	IPAddress address;
	int protocol = 0;
	bool full = false;
	bool empty = false;

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
	
	public bool IsFull() {
		return full;
	}
	
	public bool IsEmpty() {
		return empty;
	}
	
	private string port_in_hex() {
		return string.Format("{0:x2}", this.port);
	}
	
	public void SetProtocol(string protocol) {
		Masterserver.DebugMessage("SetProtocol(string protocol)");
		if (!Int32.TryParse(protocol, out this.protocol)) {
			this.protocol = 0;
		}
		Masterserver.DebugMessage("Set protocol to: "+this.protocol);
	}

	public void SetProtocol(int protocol) {
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
		return this.server_entry_in_hex();
	}
	
	public Thread QueryDataThreaded() {
		Thread thread = new Thread(new ThreadStart(this.QueryData));
		thread.Start();
        return thread;
	}
	
	public void QueryData() {
		Masterserver.DebugMessage("Querying server "+this+"...");
		int destination_port = (int)this.port;
		IPAddress destination_ip = this.address;
        UdpClient udpClient = null;
        try{
            udpClient = Masterserver.NewLocalClient();
        }
        catch (Exception e) {
            Console.WriteLine("Could not get an open connection to destination game server {0}:{1}.", destination_ip, destination_port);
			Console.WriteLine(e.Message);
            return;
        }
		try{
			udpClient.Connect(destination_ip, destination_port);
			Masterserver.DebugMessage("Got Connect to "+this.ToString());
			byte[] server_status_query_head = QueryStrings.GetArray("server_status_query_head");
			byte[] server_response_head = QueryStrings.GetArray("server_status_answer_head");
			int server_response_head_length = Encoding.ASCII.GetString(server_response_head).Length;

            byte[] sendBytes = server_status_query_head;
			Masterserver.DebugMessage("Sending to "+this.ToString()+"...");
            udpClient.Send(sendBytes, sendBytes.Length);

			Masterserver.DebugMessage("Waiting for response from "+this.ToString()+"...");
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(destination_ip, destination_port);

            // Won't block the entire program, but requires a reasonable timeout value
			var asyncResult = udpClient.BeginReceive( null, null );
			asyncResult.AsyncWaitHandle.WaitOne(Masterserver.timeoutms);
			byte[] receiveBytes = null;
			if (asyncResult.IsCompleted)
			{
				try
				{
					receiveBytes = udpClient.EndReceive(asyncResult, ref RemoteIpEndPoint);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					udpClient.Close();
					return;
				}
			}
			if (receiveBytes == null) {
				Masterserver.DebugMessage("Nothing ever came from "+this.ToString()+".");
				udpClient.Close();
				return;
			}
			udpClient.Close();
            Byte[] start = receiveBytes.Take(server_response_head_length).ToArray();
            Byte[] ende  = receiveBytes.Skip(server_response_head_length+1).ToArray();
            
            Masterserver.DebugMessage("Received data from "+this.ToString()+".");
            if (start.SequenceEqual(server_response_head)) {
				Masterserver.DebugMessage("Found an expected header from "+this.ToString()+".");
                string returnData = Encoding.ASCII.GetString(ende);
                string[] bloecke = returnData.Split('"');
                if (bloecke.Length != 2) {
					this.protocol = 0;
					return;
				}
				string datenblock = bloecke[0].Substring(1);
				Masterserver.DebugMessage("Received data '"+datenblock+"' from "+this.ToString());
				List<string> parameter_liste = datenblock.Split('\\').ToList();
				if (parameter_liste.Count() % 2 == 1) {
					this.protocol = 0;
					return;
				}
				List<string>.Enumerator parameter_enumerator = parameter_liste.GetEnumerator();
				Dictionary<string, string> parameter_hash = new Dictionary<string, string>();
				while (parameter_enumerator.MoveNext()) { 
					string key = parameter_enumerator.Current;
					parameter_enumerator.MoveNext();
					string wert = parameter_enumerator.Current;
					if (parameter_hash.ContainsKey(key)) {
						parameter_hash.Remove(key);
					}
					parameter_hash.Add(key, wert);
				}
				string protocol;
				if (!parameter_hash.TryGetValue("protocol", out protocol)) {
					Masterserver.DebugMessage("Didn't receive any protocol from "+this.ToString()+".");
					SetProtocol("0");
				} else {
					Masterserver.DebugMessage("Protocol "+protocol+" received from "+this.ToString()+".");
					SetProtocol(protocol);
				}
				string sv_maxclients;
				string clients;
				if (!parameter_hash.TryGetValue("sv_maxclients", out sv_maxclients)) {
					this.full = false;
					this.empty = false;
				} else if (!parameter_hash.TryGetValue("clients", out clients)) {
					this.full = false;
					this.empty = false;
				} else {
					int clients_n = int.Parse(clients);
					int sv_maxclients_n = int.Parse(clients);
					if (clients_n == 0) {
						this.empty = true;
					}
					if (clients_n.Equals(sv_maxclients_n)) {
						this.full = true;
					}
				}
				return;
            } else {
				Masterserver.DebugMessage("Unrecognized response '"+Encoding.ASCII.GetString(receiveBytes)+"' from "+this.ToString());
				this.protocol = 0;
				udpClient.Close();
				return;
			}

        }  
        catch (Exception e) {
			Console.WriteLine(e.Message);
            return;
        }
	}
}
