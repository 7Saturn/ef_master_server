using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
public static class Parser {
    public static int HexToDec(string hexValue) {
        int number = 0;
        try {
            number = Int32.Parse(hexValue,
                                 System.Globalization.NumberStyles.HexNumber);
        }
        catch(Exception e) {
            Printer.VerboseMessage(e.ToString());
        }
        return number;
    }

    public static Dictionary <string,string> SplitStringToParameters(string data) {
        Dictionary <string,string> queryValues =
            new Dictionary <string,string>();
        List<string> parameterList = data.Split(
            new [] { '\\' },
            StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parameterList.Count == 0) {
            return null;
        }
        if (parameterList.Count() % 2 == 1) {
            Printer.DebugMessage(
                "Warning: Got an uneven number of parameters from string '"
                + data + "', something is wrong here.");
            return null;
        }
        List<string>.Enumerator parameterEnumerator =
            parameterList.GetEnumerator();
        while (parameterEnumerator.MoveNext()) {
            string key = parameterEnumerator.Current;
            parameterEnumerator.MoveNext();
            string wert = parameterEnumerator.Current;
            if (queryValues.ContainsKey(key)) {
                queryValues.Remove(key);
            }
            if (!key.Equals("challenge")) {
                queryValues.Add(key, wert);
            }
        }
        return queryValues;
    }

    public static string GetDataFromDetails(string data) {
        List<string> parameterList = data.Split('\n').ToList();
        return parameterList.First();
    }

    public static List<Player> GetPlayersFromDetails(string data) {
        List<string> parameterList = data.Split('\n').ToList();
        parameterList.RemoveAt(0);
        List<Player> playerlist = new List<Player>();
        foreach (string playerEntry in parameterList) {
            List<string> playerData = playerEntry.Split(' ').ToList();
            if (playerData.Count() == 3) {
                int frags = 0;
                Int32.TryParse(playerData.First(), out frags);
                int ping = 0;
                Int32.TryParse(playerData.ElementAt(1), out ping);
                string nick = playerData.ElementAt(2);
                nick = nick.Substring(1,nick.Length-1);
                Player newguy = new Player(frags, ping, nick);
                playerlist.Add(newguy);
            }
        }
        return playerlist;
    }

    public static Dictionary <string,string> ConcatDictionaries(
        Dictionary <string,string> first,
        Dictionary <string,string> second) {
        Dictionary <string,string> third = new Dictionary <string,string>();
        foreach (KeyValuePair<string, string> item in first) {
            if (!second.ContainsKey(item.Key)) {
                third.Add(item.Key,item.Value);
            }
        }
        foreach (KeyValuePair<string, string> item in second) {
            third.Add(item.Key,item.Value);
        }
        Printer.DebugMessage ("ConcatDictionaries");
        if (Printer.GetDebug()) {
            Printer.DumpDictionary(first);
            Printer.DumpDictionary(second);
            Printer.DumpDictionary(third);
        }
        return third;
    }

    public static string getEFIpPortString(Byte[] ipAndPort) {
        // In case we were fed BS, this will return 0.0.0.0
        int ip1 = Parser.HexToDec(
            Encoding.ASCII.GetString(
                QueryStrings.GetSubByteArray(ipAndPort, 0, 2)));
        int ip2 = Parser.HexToDec(
            Encoding.ASCII.GetString(
                QueryStrings.GetSubByteArray(ipAndPort, 2, 2)));
        int ip3 = Parser.HexToDec(
            Encoding.ASCII.GetString(
                QueryStrings.GetSubByteArray(ipAndPort, 4, 2)));
        int ip4 = Parser.HexToDec(
            Encoding.ASCII.GetString(
                QueryStrings.GetSubByteArray(ipAndPort, 6, 2)));
        return ip1 + "." + ip2 + "." + ip3 + "." + ip4;
    }

    public static string getEFIpPortString(string ipAndPort) {
        // In case we were fed BS, this will return 0.0.0.0
        int ip1 = Parser.HexToDec(ipAndPort.Substring(0,2));
        int ip2 = Parser.HexToDec(ipAndPort.Substring(2,2));
        int ip3 = Parser.HexToDec(ipAndPort.Substring(4,2));
        int ip4 = Parser.HexToDec(ipAndPort.Substring(6,2));
        return ip1 + "." + ip2 + "." + ip3 + "." + ip4;
    }

    public static bool ByteArraysAreEqual(byte[] longer, byte[] shorter) {
        byte[] longerRest = longer.Take(shorter.Length).ToArray();
        return longerRest.SequenceEqual(shorter);
    }

    public static byte[] ConcatByteArray(byte[][] arraylist) {
        List<byte> temporaryList = new List<byte>();
        foreach (byte[] block in arraylist) {
            temporaryList.AddRange(block);
        }
        return temporaryList.ToArray();
    }
}
