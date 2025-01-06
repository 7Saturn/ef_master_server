public class Player {
    private int frags = 0;
    private int ping = 0;
    private string nick = "";

    public Player (int newFrags, int newPing, string newNick) {
        this.frags = newFrags;
        this.ping = newPing;
        this.nick = newNick;
    }

    //Currently unused:
    public int GetFrags() {
        return this.frags;
    }

    //Currently unused:
    public int GetPing() {
        return this.ping;
    }

    //Currently unused:
    public string GetNick() {
        return this.nick;
    }

    public override string ToString() {
        return this.nick + ": " + frags;
    }
}
