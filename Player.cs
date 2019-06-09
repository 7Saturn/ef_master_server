public class Player {
    private int frags = 0;
    private int ping = 0;
    private string nick = "";
    
    public Player (int new_frags, int new_ping, string new_nick) {
        this.frags = new_frags;
        this.ping = new_ping;
        this.nick = new_nick;
    }
    
    public int GetFrags() {
        return this.frags;
    }

    public int GetPing() {
        return this.ping;
    }

    public string GetNick() {
        return this.nick;
    }
    
    public override string ToString() {
        return this.nick+": "+frags;
    }
}
