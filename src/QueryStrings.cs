using System.Collections.Generic;
using System.Text;

public static class QueryStrings {

	private static Dictionary<string,byte[]> mapping = null;

    private static void CreateMapping() {
        mapping = new Dictionary<string,byte[]>();
        byte[] yyyy = new byte[4] {255,255,255,255};
        byte[] getservers = new byte[10] {103, 101, 116, 115, 101, 114, 118, 101, 114, 115};
        byte[] getallservers = new byte[13] {103, 101, 116, 97, 108, 108, 115, 101, 114, 118, 101, 114, 115};
        byte[] space = new byte[1] {32};
        byte[] empty = new byte[5] {101, 109, 112, 116, 121};
        byte[] full = new byte[4] {102, 117, 108, 108};
        byte[] getserversResponse = new byte[] {103, 101, 116, 115, 101, 114, 118, 101, 114, 115, 082, 101, 115, 112, 111, 110, 115, 101};
        byte[] eot = new byte[] {92, 069, 079, 084};
        byte[] heartbeat = new byte [] {92, 104, 101, 097, 114, 116, 098, 101, 097, 116, 92};
        byte[] gamename = new byte[] {092, 103, 097, 109, 101, 110, 097, 109, 101, 092, 083, 084, 069, 070, 049, 092};
        byte[] getinfo_xxx = new byte[] {103, 101, 116, 105, 110, 102, 111, 32, 120, 120, 120};
        byte[] getstatus = new byte [] {103, 101, 116, 115, 116, 097, 116, 117, 115};
        byte[] statusResponse = new byte[] {115, 116, 097, 116, 117, 115, 082, 101, 115, 112, 111, 110, 115, 101};
        byte[] inforesponse = new byte[] {105, 110, 102, 111, 82, 101, 115, 112, 111, 110, 115, 101, 32};

        byte[] server_status_query_head = ConcatByteArray(new byte[][] {yyyy, getinfo_xxx});
        byte[] server_details_query_head = ConcatByteArray(new byte[][] {yyyy, getstatus});
        byte[] server_status_answer_head = ConcatByteArray(new byte[][] {yyyy, inforesponse});
        byte[] server_details_answer_head = ConcatByteArray(new byte[][] {yyyy, statusResponse, new byte[] {10}});
        byte[] server_list_query_head = ConcatByteArray(new byte[][] {yyyy, getservers, space});
        byte[] server_list_all_query_head = ConcatByteArray(new byte[][] {yyyy, getallservers, space});
        byte[] server_list_response_head = ConcatByteArray(new byte[][] {yyyy, getserversResponse});
        byte[] server_list_response_head_space = ConcatByteArray(new byte[][] {yyyy, getserversResponse, space});
        byte[] heartbeat_signal_head = ConcatByteArray(new byte[][] {yyyy, heartbeat});
        byte[] heartbeat_signal_tail = ConcatByteArray(new byte[][] {gamename});

        mapping.Add("server_status_query_head", server_status_query_head);
        mapping.Add("server_status_answer_head", server_status_answer_head);
        mapping.Add("server_details_query_head", server_details_query_head);
        mapping.Add("server_details_answer_head", server_details_answer_head);
        mapping.Add("server_list_query_head", server_list_query_head);
        mapping.Add("server_list_all_query_head", server_list_all_query_head);
        mapping.Add("server_list_response_head", server_list_response_head);
        mapping.Add("server_list_response_head_space", server_list_response_head_space);
        mapping.Add("heartbeat_signal_head", heartbeat_signal_head);
        mapping.Add("heartbeat_signal_tail", heartbeat_signal_tail);
        mapping.Add("empty", empty);
        mapping.Add("full", full);
        mapping.Add("eot", eot);
    }

	private static Dictionary<string,byte[]> GetMapping() {
		if (mapping == null) {
			CreateMapping();
		}
        return mapping;
	}

	public static byte[] GetArray(string name) {
		Dictionary<string,byte[]> mapping = QueryStrings.GetMapping();
		byte[] return_string = null;
        if (!mapping.TryGetValue(name, out return_string)) {
			throw new StringNameInvalidException("There is no server query with name '" + name + "'.");
		}
        else {
			return return_string;
		}
	}
    public static byte[] GetHeartbeatComparison(ushort port) {
        byte[] heartbeat_head = QueryStrings.GetArray("heartbeat_signal_head");
        byte[] heartbeat_port = Encoding.ASCII.GetBytes(port.ToString());
        byte[] heartbeat_tail = QueryStrings.GetArray("heartbeat_signal_tail");
        return ConcatByteArray(new byte[][] {heartbeat_head, heartbeat_port, heartbeat_tail});
    }

	public static byte[] ConcatByteArray (byte[][] arraylist) {
		List<byte> temp_list = new List<byte>();
		foreach (byte[] block in arraylist) {
			temp_list.AddRange(block);
		}
		return temp_list.ToArray();
	}
}
