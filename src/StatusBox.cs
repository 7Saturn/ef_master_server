using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

public class StatusBox : Form {
    private Gui origin;
    private Label versionLabel = new Label();
    private Label versionText = new Label();
    private Label portLabel = new Label();
    private Label portText = new Label();
    private Label verboseLabel = new Label();
    private Label verboseText = new Label();
    private Label debugLabel = new Label();
    private Label debugText = new Label();
    private Label intervalLabel = new Label();
    private Label intervalText = new Label();
    private Label interfaceLabel = new Label();
    private Label interfaceText = new Label();
    private Label MasterServerListLabel = new Label();
    private TextBox masterServerList = new TextBox();
    public const int leftColumnWith = 190;
    public const int rightColumnWith = 230;

    public StatusBox(Gui sourceWindow) {
        if (sourceWindow.Icon != null) {
            this.Icon = sourceWindow.Icon;
        }

        this.origin = sourceWindow;
        Printer.DebugMessage("Creating status window...");
        this.Size = new Size(429, 358);
        this.Text = "Status of EF Masterserver";
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.ShowInTaskbar = true;
        CenterToScreen();

        ToolTip buttonTooltip = new ToolTip(); //Can be used multiple times
        buttonTooltip.SetToolTip(this,
                                 "Here you can see the current"
                                 + " configuration/status of the master"
                                 + " server."); //Window explains itself. ;-)


        Button closeButton = new Button();
        closeButton.Text = "Close";
        this.Controls.Add(closeButton);
        closeButton.Parent = this;
        CancelButton = closeButton;
        closeButton.Click += new EventHandler (CloseThis);
        buttonTooltip.SetToolTip(closeButton,
                                 "Closes this window and shows server list"
                                 + " (ESC/Enter).");
        Gui.CenterButton(closeButton);
        Gui.BottomButton(closeButton);

        versionLabel.Location = new Point(0,0);
        versionLabel.Height = 20;
        versionLabel.AutoSize = false;
        versionLabel.Width = leftColumnWith;
        versionLabel.Text = "Version of Masterserver:";
        versionLabel.Parent = this;
        buttonTooltip.SetToolTip(versionLabel,
                                 "This is the version of this program.");

        versionText.Location = new Point(leftColumnWith,0);
        versionText.Height = 20;
        versionText.AutoSize = false;
        versionText.Width = rightColumnWith;
        versionText.Text = Masterserver.GetVersionString();
        versionText.Parent = this;
        buttonTooltip.SetToolTip(versionText,
                                 "This is the version of this program.");


        portLabel.Location = new Point(0,20);
        portLabel.Height = 20;
        portLabel.AutoSize = false;
        portLabel.Width = leftColumnWith;
        portLabel.Text = "Used port:";
        portLabel.Parent = this;
        buttonTooltip.SetToolTip(portLabel,
                                 "This is/are the network port(s) the server"
                                 + " is listening on.");

        portText.Location = new Point(leftColumnWith,20);
        portText.Height = 20;
        portText.AutoSize = false;
        portText.Width = rightColumnWith;
        string port = Masterserver.GetPortV4().ToString() + " (UDPv4)";
        if (Masterserver.InV6Mode()) {
            port += ", " + Masterserver.GetPortV6().ToString() + " (UDPv6)";
        }
        portText.Text = port;
        portText.Parent = this;
        buttonTooltip.SetToolTip(portText,
                                 "This displays the network port(s) the"
                                 + " server is listening on.");


        verboseLabel.Location = new Point(0,40);
        verboseLabel.Height = 20;
        verboseLabel.AutoSize = false;
        verboseLabel.Width = leftColumnWith;
        verboseLabel.Text = "Console output is verbose:";
        verboseLabel.Parent = this;
        buttonTooltip.SetToolTip(verboseLabel,
                                 "Is the console output a little more"
                                 + " elaborate?");

        verboseText.Location = new Point(leftColumnWith,40);
        verboseText.Height = 20;
        verboseText.AutoSize = false;
        verboseText.Width = rightColumnWith;
        verboseText.Text = Printer.GetVerbose() ? "yes" : "no";
        verboseText.Parent = this;
        buttonTooltip.SetToolTip(verboseText,
                                 "Is the console output a little more"
                                 + " elaborate?");


        debugLabel.Location = new Point(0,60);
        debugLabel.Height = 20;
        debugLabel.AutoSize = false;
        debugLabel.Width = leftColumnWith;
        debugLabel.Text = "Using debug output on console:";
        debugLabel.Parent = this;
        buttonTooltip.SetToolTip(debugLabel,
                                 "Is the console output very detailed?");

        debugText.Location = new Point(leftColumnWith,60);
        debugText.Height = 20;
        debugText.AutoSize = false;
        debugText.Width = rightColumnWith;
        debugText.Text = Printer.GetDebug() ? "yes" : "no";
        debugText.Parent = this;
        buttonTooltip.SetToolTip(debugText,
                                 "Is the console output very detailed?");


        interfaceLabel.Location = new Point(0,80);
        interfaceLabel.Height = 20;
        interfaceLabel.AutoSize = false;
        interfaceLabel.Width = leftColumnWith;
        interfaceLabel.Text = "Master server listening on:";
        interfaceLabel.Parent = this;
        buttonTooltip.SetToolTip(interfaceLabel,
                                 "Network interface(s) the master server is"
                                 + " listening on.");

        interfaceText.Location = new Point(leftColumnWith,80);
        interfaceText.Height = 20;
        interfaceText.AutoSize = false;
        interfaceText.Width = rightColumnWith;
        string listeningInterfaces =
            Masterserver.GetMasterServerListeningInterfaceV4();
        if (Masterserver.InV6Mode()) {
            listeningInterfaces += ", ";
            listeningInterfaces +=
                Masterserver.GetMasterServerListeningInterfaceV6();
        }
        interfaceText.Text = listeningInterfaces;
        interfaceText.Parent = this;
        buttonTooltip.SetToolTip(interfaceText,
                                 "Network interface(s) the master server is"
                                 + " listening on.");


        MasterServerListLabel.Location = new Point(0,100);
        MasterServerListLabel.Height = 20;
        MasterServerListLabel.AutoSize = false;
        MasterServerListLabel.Width = leftColumnWith;
        MasterServerListLabel.Text = "Master server list:";
        MasterServerListLabel.Parent = this;
        buttonTooltip.SetToolTip(MasterServerListLabel,
                                 "What other master servers are queried?");

        string masterServerListString = "";
        string[] masterServerListStrings = Masterserver.GetMasterServerSources();
        if (   masterServerListStrings == null
            || masterServerListStrings.Length == 0) {
            masterServerListString = "No servers provided, feature inactive";
        }
        else if (masterServerListStrings.Length == 1) {
            masterServerListString = masterServerListStrings[0];
        }
        else {
            masterServerListString = String.Join("\n", masterServerListStrings);
        }
        this.masterServerList.Text = masterServerListString;
        this.masterServerList.Location = new Point (leftColumnWith + 1, 100);
        this.masterServerList.Width = rightColumnWith;
        this.masterServerList.Height = 178;
        this.masterServerList.AcceptsReturn = true;
        this.masterServerList.AcceptsTab = false;
        this.masterServerList.Multiline = true;
        this.masterServerList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        buttonTooltip.SetToolTip(masterServerList,
                                 "What other master servers are queried?");
        masterServerList.Parent = this;


        intervalLabel.Location = new Point(0,280);
        intervalLabel.Height = 20;
        intervalLabel.AutoSize = false;
        intervalLabel.Width = leftColumnWith;
        intervalLabel.Text = "Master server query interval:";
        intervalLabel.Parent = this;
        buttonTooltip.SetToolTip(intervalLabel,
                                 "Interval other master servers are queried.");

        intervalText.Location = new Point(leftColumnWith,280);
        intervalText.Height = 20;
        intervalText.AutoSize = false;
        intervalText.Width = rightColumnWith;
        if (Masterserver.GetMasterServerQueryInterval() > 0) {
            intervalText.Text =
                Masterserver.GetMasterServerQueryInterval().ToString()
                + " Seconds";
        }
        else {
            if (   masterServerListStrings != null
                && masterServerListStrings.Length != 0) {
                intervalText.Text = "Not applied (feature not active)";
            }
            else {
                intervalText.Text = "Not applied (query only once at startup)";
            }
        }
        intervalText.Parent = this;
        buttonTooltip.SetToolTip(intervalText,
                                 "Interval other master servers are queried.");
    }

    private void CloseThis(object sender, EventArgs e) {
        Printer.DebugMessage("Showing main window...");
        origin.Show();
        Printer.DebugMessage("Closing status window...");
        this.Close();
    }
}
