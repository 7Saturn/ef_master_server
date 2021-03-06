using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
public static class Parser {
	public static int HexToDec(string hexValue)
	{
		return Int32.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
	}

    public static Dictionary <string,string> SplitStringToParameters (string data) {
        Dictionary <string,string> query_values = new Dictionary <string,string>();
        List<string> parameter_list = data.Split(new [] { '\\' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parameter_list.Count == 0) {
            return null;
        }
        if (parameter_list.Count() % 2 == 1) {
            Printer.DebugMessage("Warning: Got an uneven number of parameters from string '" + data + "', something is wrong here.");
            return null;
        }
        List<string>.Enumerator parameter_enumerator = parameter_list.GetEnumerator();
        while (parameter_enumerator.MoveNext()) {
            string key = parameter_enumerator.Current;
            parameter_enumerator.MoveNext();
            string wert = parameter_enumerator.Current;
            if (query_values.ContainsKey(key)) {
                query_values.Remove(key);
            }
            if (!key.Equals("challenge")) {
                query_values.Add(key, wert);
            }
        }
        return query_values;
    }

    public static string GetDataFromDetails (string data) {
        List<string> parameter_list = data.Split('\n').ToList();
        return parameter_list.First();
    }

    public static List<Player> GetPlayersFromDetails (string data) {
        List<string> parameter_list = data.Split('\n').ToList();
        parameter_list.RemoveAt(0);
        List<Player> playerlist = new List<Player>();
        foreach (string playerEntry in parameter_list) {
            List<string> player_data = playerEntry.Split(' ').ToList();
            if (player_data.Count() == 3) {
                int frags = 0;
                Int32.TryParse(player_data.First(), out frags);
                int ping = 0;
                Int32.TryParse(player_data.ElementAt(1), out ping);
                string nick = player_data.ElementAt(2);
                nick = nick.Substring(1,nick.Length-1);
                Player newguy = new Player(frags, ping, nick);
                playerlist.Add(newguy);
            }
        }
        return playerlist;
    }

    public static Dictionary <string,string> ConcatDictionaries(Dictionary <string,string> first,
                                                                Dictionary <string,string> second)
    {
        Dictionary <string,string> third = new Dictionary <string,string>();
        foreach (KeyValuePair<string, string> item in first)
        {
            if (!second.ContainsKey(item.Key)) {
                third.Add(item.Key,item.Value);
            }
        }
        foreach (KeyValuePair<string, string> item in second)
        {
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

}
