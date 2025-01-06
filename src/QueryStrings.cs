using System;
using System.Collections.Generic;
using System.Text;
using System.Linq; // Skip/Take

public static class QueryStrings {

    public enum requestType {
        dump,
        heartbeat,
        listIpV4,
        listIpV6,
        none
    }

    public enum stringType {
        server_status_query_head,
        server_status_answer_head,
        server_details_query_head,
        server_details_answer_head,
        server_list_query_head_v4,
        server_list_query_head_v6,
        server_list_all_query_head,
        server_list_response_head_v4,
        server_list_response_head_v6,
        server_list_response_head_space_v4,
        heartbeat_signal_head,
        heartbeat_signal_tail,
        empty,
        full,
        ipv6,
        eot
    }

    private static Dictionary<stringType,byte[]> mapping = null;

    // Make 100% sure, you use that only once!
    public static void CreateMapping() {
        if (mapping != null) {
            return;
        }
        mapping = new Dictionary<stringType,byte[]>();
        byte[] yyyy                 = new byte[] {255, 255, 255, 255};
        byte[] space                = new byte[] {32};
        byte[] line_feed            = new byte[] {10};
        //                                        g    e    t    a   l    l    s    e    r    v    e    r    s
        byte[] getallservers        = new byte[] {103, 101, 116, 97, 108, 108, 115, 101, 114, 118, 101, 114, 115};
        //                                        e    m    p    t    y
        byte[] empty                = new byte[] {101, 109, 112, 116, 121};
        //                                        f    u    l    l
        byte[] full                 = new byte[] {102, 117, 108, 108};
        //                                        \   E    O    T
        byte[] eot                  = new byte[] {92, 069, 079, 084};
        //                                        \   h    e    a    r    t    b    e    a    t    \
        byte[] heartbeat            = new byte[] {92, 104, 101, 097, 114, 116, 098, 101, 097, 116, 92};
        //                                        \    g    a    m    e    n    a    m    e    \    S    T    E    F    1    \
        byte[] gamename             = new byte[] {092, 103, 097, 109, 101, 110, 097, 109, 101, 092, 083, 084, 069, 070, 049, 092};
        //                                        g    e    t    i    n    f    o    ' ' x    x    x
        byte[] getinfo_xxx          = new byte[] {103, 101, 116, 105, 110, 102, 111, 32, 120, 120, 120};
        //                                        g    e    t    s    t    a    t    u    s
        byte[] getstatus            = new byte[] {103, 101, 116, 115, 116, 097, 116, 117, 115};
        //                                        s    t    a    t    u    s    R    e    s    p    o    n    s    e
        byte[] statusResponse       = new byte[] {115, 116, 097, 116, 117, 115, 082, 101, 115, 112, 111, 110, 115, 101};
        //                                        i    n    f    o    r   e    s    p    o    n    s    e    ' '
        byte[] inforesponse         = new byte[] {105, 110, 102, 111, 82, 101, 115, 112, 111, 110, 115, 101, 32};
        //                                        g    e    t    s    e    r    v    e    r    s
        byte[] getserversV4         = new byte[] {103, 101, 116, 115, 101, 114, 118, 101, 114, 115};
        //                                        g    e    t    s    e    r    v    e    r    s    E   x    t    ' ' E   l    i    t    e    F   o    r    c   e
        byte[] getserversV6         = new byte[] {103, 101, 116, 115, 101, 114, 118, 101, 114, 115, 69, 120, 116, 32, 69, 108, 105, 116, 101, 70, 111, 114, 99, 101};
        //                                        i    p    v    6
        byte [] ipv6                = new byte[] {105, 112, 118, 54};
        //                                        g    e    t    s    e    r    v    e    r    s    R    e    s    p    o    n    s    e
        byte[] getserversResponseV4 = new byte[] {103, 101, 116, 115, 101, 114, 118, 101, 114, 115, 082, 101, 115, 112, 111, 110, 115, 101};
        //                                        g    e    t    s    e    r    v    e    r    s    E    x    t    R    e    s    p    o    n    s    e
        byte[] getserversResponseV6 = new byte[] {103, 101, 116, 115, 101, 114, 118, 101, 114, 115, 069, 120, 116, 082, 101, 115, 112, 111, 110, 115, 101};




        byte[] server_status_query_head =           Parser.ConcatByteArray(new byte[][] {yyyy, getinfo_xxx});
        byte[] server_details_query_head =          Parser.ConcatByteArray(new byte[][] {yyyy, getstatus});
        byte[] server_status_answer_head =          Parser.ConcatByteArray(new byte[][] {yyyy, inforesponse});
        byte[] server_details_answer_head =         Parser.ConcatByteArray(new byte[][] {yyyy, statusResponse, line_feed});
        byte[] server_list_query_head_v4 =          Parser.ConcatByteArray(new byte[][] {yyyy, getserversV4, space});
        byte[] server_list_query_head_v6 =          Parser.ConcatByteArray(new byte[][] {yyyy, getserversV6, space});
        byte[] server_list_all_query_head =         Parser.ConcatByteArray(new byte[][] {yyyy, getallservers, space});
        byte[] server_list_response_head_space_v4 = Parser.ConcatByteArray(new byte[][] {yyyy, getserversResponseV4, space});
        byte[] server_list_response_head_v4 =       Parser.ConcatByteArray(new byte[][] {yyyy, getserversResponseV4});
        byte[] server_list_response_head_v6 =       Parser.ConcatByteArray(new byte[][] {yyyy, getserversResponseV6});
        byte[] heartbeat_signal_head =              Parser.ConcatByteArray(new byte[][] {yyyy, heartbeat});
        byte[] heartbeat_signal_tail =              Parser.ConcatByteArray(new byte[][] {gamename});

        mapping.Add(stringType.server_status_query_head,           server_status_query_head);
        mapping.Add(stringType.server_status_answer_head,          server_status_answer_head);
        mapping.Add(stringType.server_details_query_head,          server_details_query_head);
        mapping.Add(stringType.server_details_answer_head,         server_details_answer_head);
        mapping.Add(stringType.server_list_query_head_v4,          server_list_query_head_v4);
        mapping.Add(stringType.server_list_query_head_v6,          server_list_query_head_v6);
        mapping.Add(stringType.server_list_all_query_head,         server_list_all_query_head);
        mapping.Add(stringType.server_list_response_head_v4,       server_list_response_head_v4);
        mapping.Add(stringType.server_list_response_head_v6,       server_list_response_head_v6);
        mapping.Add(stringType.server_list_response_head_space_v4, server_list_response_head_space_v4);
        mapping.Add(stringType.heartbeat_signal_head,              heartbeat_signal_head);
        mapping.Add(stringType.heartbeat_signal_tail,              heartbeat_signal_tail);
        mapping.Add(stringType.empty,                              empty);
        mapping.Add(stringType.full,                               full);
        mapping.Add(stringType.ipv6,                               ipv6);
        mapping.Add(stringType.eot,                                eot);
    }

    private static Dictionary<stringType,byte[]> GetMapping() {
        if (mapping == null) {
            CreateMapping();
        }
        return mapping;
    }

    public static byte[] GetByteArray(stringType name) {
        Dictionary<stringType,byte[]> mapping = QueryStrings.GetMapping();
        byte[] return_string = null;
        mapping.TryGetValue(name, out return_string);
        return return_string;
    }

    public static Byte[] GetSubByteArray(Byte[] input,
                                         int startIndex,
                                         int length) {
        if (   input == null
            || (startIndex + length > input.Length)
            || startIndex < 0
            || length < 0) {
            return null;
        }
        Byte[] rest = input.Skip(startIndex).ToArray();
        Byte[] want = rest.Take(length).ToArray();
        return want;
    }

    public static byte[] GetHeartbeatComparison(ushort port) {
        byte[] heartbeat_head = QueryStrings.GetByteArray(
            stringType.heartbeat_signal_head);
        byte[] heartbeat_port = Encoding.ASCII.GetBytes(port.ToString());
        byte[] heartbeat_tail = QueryStrings.GetByteArray(
            stringType.heartbeat_signal_tail);
        return Parser.ConcatByteArray(new byte[][] {heartbeat_head,
                                                    heartbeat_port,
                                                    heartbeat_tail});
    }

    public static byte[] GetServerListRequestV4(byte[] version) {
        byte[] empty = GetByteArray(stringType.empty);
        byte[] full = GetByteArray(stringType.full);
        byte[] space = {32};
        byte[] serverListQueryHeadV4 = GetByteArray(
            stringType.server_list_query_head_v4);
        return Parser.ConcatByteArray(new byte[][] {serverListQueryHeadV4,
                                                    version,
                                                    space,
                                                    full,
                                                    space,
                                                    empty});
    }

    public static byte[][] GetServerListRequestsV4() {
        byte[] version22 = {50, 50};
        byte[] version23 = {50, 51};
        byte[] version24 = {50, 52};
        byte[][] serverListRequestsV4 = {
            GetServerListRequestV4(version22),
            GetServerListRequestV4(version23),
            GetServerListRequestV4(version24)
        };
        return serverListRequestsV4;
    }

    public static byte[] GetServerListRequestV6() {
        byte[] empty = GetByteArray(stringType.empty);
        byte[] full = GetByteArray(stringType.full);
        byte[] space = {32};
        byte[] version24 = {50, 52};
        byte[] serverListQueryHeadV6 = GetByteArray(stringType.server_list_query_head_v6);
        byte[] serverListRequestV6 = Parser.ConcatByteArray(
            new byte[][] {
            serverListQueryHeadV6,
            version24,
            space,
            full,
            space,
            empty
        });
        return serverListRequestV6;
    }

    public static byte[] GetRandomByteArray(int maxLength) {
        Random rnd = new Random();
        int noOfBytes = rnd.Next(maxLength) + 1;
        return GetRandomByteArrayFixedSize(noOfBytes);
    }

    public static byte[] GetRandomByteArrayFixedSize(int length) {
        Random rnd = new Random();
        List<byte> byteList = new List<byte>();
        for (int counter = 0; counter < length; counter++) {
            int randomByte = rnd.Next(256);
            byteList.Add((byte) randomByte);
        }
        return byteList.ToArray();
    }

    /* It is not enough to simply compare the strings! E.g. the four 0x255
       characters turn into question marks, which in turn would equal literal ?,
       too, although they are not the same. So using byte arrays. That works
       properly. */
    public static bool IsHeartbeatRequest(byte[] received, ushort port) {
        Printer.DebugMessage("IsHeartbeatRequest?\nreceived: '"
                             + Encoding.ASCII.GetString(received)
                             + "', port: " + port);
        if (received == null) {
            Printer.DebugMessage("Working on nothing, so no.");
            return false;
        }
        byte[] heartbeatSignal = QueryStrings.GetHeartbeatComparison(port);
        Printer.DebugMessage("comparison: "
                             + Encoding.ASCII.GetString(heartbeatSignal));
        if (received.Length < heartbeatSignal.Length) {
            Printer.DebugMessage("Too short, can't be!");
            return false;
        }
        bool result = Parser.ByteArraysAreEqual(received, heartbeatSignal);
        if (result) {
            Printer.DebugMessage("It is.");
        }
        else {
            Printer.DebugMessage("It's not.");
        }
        return result;
    }

    public static bool IsDumpRequest(byte[] received) {
        Printer.DebugMessage("IsDumpRequest?"
                             + "\nreceived: '"
                             + Encoding.ASCII.GetString(received) + "'");
        if (received == null) {
            Printer.DebugMessage("Working on nothing, so no.");
            return false;
        }
        byte[] serverListAllQueryHead =
            QueryStrings.GetByteArray(
                QueryStrings.stringType.server_list_all_query_head);
        Printer.DebugMessage("comparison: "
                             + Encoding.ASCII.GetString(serverListAllQueryHead));
        if (received.Length < serverListAllQueryHead.Length) {
            Printer.DebugMessage("Too short, can't be!");
            return false;
        }
        bool result = Parser.ByteArraysAreEqual(received,
                                                serverListAllQueryHead);
        if (result) {
            Printer.DebugMessage("It is.");
        }
        else {
            Printer.DebugMessage("It's not.");
        }
        return result;
    }

    private static QueryStrings.requestType GetListRequestType(byte[] received,
                                                               bool inV6Mode) {
        Printer.DebugMessage("GetListRequestType?"
                             + "\nreceived: '"
                             + Encoding.ASCII.GetString(received) + "'");
        if (inV6Mode) {
            byte[] serverListQueryHeadV6 =
                QueryStrings.GetByteArray(
                    QueryStrings.stringType.server_list_query_head_v6);
            Printer.DebugMessage("comparison for v6: "
                                 + Encoding.ASCII.GetString(
                                     serverListQueryHeadV6));
            if (   received.Length >= serverListQueryHeadV6.Length
                && Parser.ByteArraysAreEqual(received, serverListQueryHeadV6)) {
                Printer.DebugMessage("Is v6.");
                return requestType.listIpV6;
            }
        }
        byte[] serverListQueryHeadV4 =
            QueryStrings.GetByteArray(
                QueryStrings.stringType.server_list_query_head_v4);
        Printer.DebugMessage("comparison for v4: "
                             + Encoding.ASCII.GetString(serverListQueryHeadV4));
        if (received.Length < serverListQueryHeadV4.Length) {
            Printer.DebugMessage("Too short, can't be! It's none.");
            return requestType.none;
        }
        if (Parser.ByteArraysAreEqual(received, serverListQueryHeadV4)) {
            Printer.DebugMessage("Is v4.");
            return requestType.listIpV4;
        }
        Printer.DebugMessage("Is none.");
        return requestType.none;
    }

    public static requestType GetRequestType(byte[] received,
                                             ushort port,
                                             bool inV6Mode) {
        if (received == null) {
            Printer.DebugMessage("Working on nothing, so none.");
            return requestType.none;
        }
        if (QueryStrings.IsHeartbeatRequest(received, port)) {
            return requestType.heartbeat;
        }
        if (QueryStrings.IsDumpRequest(received)) {
            return requestType.dump;
        }
        return GetListRequestType(received, inV6Mode);
    }

}
