using System;
using System.Text;
using System.Collections.Generic;
public class Printer {
    private static bool debugActive = false;
    public static void SetDebug(bool active) {
        debugActive = active;
    }

    public static bool GetDebug() {
        return debugActive;
    }

    public static void DebugMessage (string debugmessage) {
        if (debugActive) {Console.WriteLine(" debug: {0}", debugmessage);}
    }

    public static void DumpDictionary(Dictionary <string,string> hash) {
        foreach (KeyValuePair<string, string> item in hash)
        {
            Console.WriteLine("'{0}' => '{1}'", item.Key, item.Value);
        }

    }

    public static void DumpBytes(byte[] receiveBytes) {
        foreach (byte zeichen in receiveBytes) {
            Console.WriteLine("{0} {1}",(int)zeichen,(char)zeichen);
        }
    }

    public static void DumpStringAsBytes(string inputstring) {
        DumpBytes(Encoding.ASCII.GetBytes(inputstring));
    }
}
