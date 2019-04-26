using System;
using System.Collections.Generic;
using System.Threading;

public static class ServerList {
	private static List<ServerEntry> serverlist = null;
	public static List<ServerEntry> get_list() {
		if (serverlist == null) {
			serverlist = new List<ServerEntry>();
		}
		return serverlist;
	}
	
	public static string ToStringList (List<ServerEntry> original_list) {
		string text_list = "";
		foreach (ServerEntry eintrag in original_list) {
			text_list += "\\"+eintrag.ToString();
		}
		return text_list;
		
	}
	
	public static string get_text_list() {
		return ToStringList(ServerList.get_list());
	}
	
	public static void AddServer(ServerEntry new_one) {
		if (!ServerList.get_list().Contains(new_one)) {
			Masterserver.DebugMessage("A new one arrived");
			ServerList.get_list().Add(new_one);
			Masterserver.DebugMessage("Now List looks like this: "+ServerList.get_text_list());
		} else {
			Masterserver.DebugMessage("A known one");
			ServerEntry old_one = ServerList.get_list().Find(x => x.Equals(new_one));
			Masterserver.DebugMessage("Old protocol: "+old_one.GetProtocol());
			Masterserver.DebugMessage("New protocol: "+new_one.GetProtocol());
			old_one.SetProtocol(new_one.GetProtocol());
		}
	}
	
	public static void RemoveServer(ServerEntry to_remove) {
		if (ServerList.get_list().Contains(to_remove)) {
			ServerList.get_list().Remove(to_remove);
		}
	}
	
	public static void Cleanup() {
        List<Thread> threadlist = new List<Thread>();
		foreach (ServerEntry serverentry in ServerList.get_list()) {
			Thread thisthread = serverentry.QueryDataThreaded();
            threadlist.Add(thisthread);
		}
        while (threadlist.Count != 0) {
            List<Thread> residualthreadlist = new List <Thread>();
            foreach(Thread thisthread in threadlist) {
                if (thisthread.IsAlive) {
                    residualthreadlist.Add(thisthread);
                }
            }
            threadlist = residualthreadlist;
        }
		List<ServerEntry> looptemp = ServerList.get_list();
		int number_of_all = ServerList.get_list().Count;
		if (number_of_all == 0) {
			return;
		}
		
		for (int counter = number_of_all-1;counter >= 0;counter--) {
			if (ServerList.get_list()[counter].GetProtocol() == 0) {
				ServerList.RemoveServer(looptemp[counter]);
			}
		}
		Masterserver.DebugMessage("Now list looks like this: "+ServerList.get_text_list());
	}
	
}
