using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

public class StatusBox : Form
{
    private Gui origin;
    private Label version_label = new Label();
    private Label version_text = new Label();
    private Label port_label = new Label();
    private Label port_text = new Label();
    private Label verbose_label = new Label();
    private Label verbose_text = new Label();
    private Label debug_label = new Label();
    private Label debug_text = new Label();
    private Label interval_label = new Label();
    private Label interval_text = new Label();
    private Label MasterServerList_label = new Label();
    private TextBox masterServerList = new TextBox();
    public const int leftColumnWith = 190;
    public const int rightColumnWith = 230;

    public StatusBox(Gui sourceWindow)
    {
        if (sourceWindow.Icon != null) {
            this.Icon = sourceWindow.Icon;
        }

        this.origin = sourceWindow;
        Printer.DebugMessage("Creating status window...");
        this.Size = new Size(429, 335);
        this.Text = "Status of Masterserver";
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.ShowInTaskbar = false;
        CenterToScreen();

        ToolTip button_tooltip = new ToolTip(); //Can be used multiple times
        button_tooltip.SetToolTip(this, "Here you can the current configuration/status of the master server"); //Window explains itself. ;-)


        Button close_button = new Button();
        close_button.Text = "Close";
        this.Controls.Add(close_button);
        close_button.Location = new Point(177, 285);
        close_button.Parent = this;
        CancelButton = close_button;
		close_button.Click += new EventHandler (CloseThis); //Event (Button_Click)

        version_label.Location = new Point(0,0);
        version_label.Height = 20;
        version_label.AutoSize = false;
        version_label.Width = leftColumnWith;
        version_label.Text = "Version of Masterserver:";
        version_label.Parent = this;
        button_tooltip.SetToolTip(version_label, "This is the version of this program.");

        version_text.Location = new Point(leftColumnWith,0);
        version_text.Height = 20;
        version_text.AutoSize = false;
        version_text.Width = rightColumnWith;
        version_text.Text = Masterserver.GetVersionString();
        version_text.Parent = this;
        button_tooltip.SetToolTip(version_text, "This is the version of this program.");

        port_label.Location = new Point(0,20);
        port_label.Height = 20;
        port_label.AutoSize = false;
        port_label.Width = leftColumnWith;
        port_label.Text = "Used port:";
        port_label.Parent = this;
        button_tooltip.SetToolTip(port_label, "This is the network port the server is listening on.");

        port_text.Location = new Point(leftColumnWith,20);
        port_text.Height = 20;
        port_text.AutoSize = false;
        port_text.Width = rightColumnWith;
        port_text.Text = Masterserver.GetPort().ToString() + " (UDP)";
        port_text.Parent = this;
        button_tooltip.SetToolTip(port_text, "This is the network port the server is listening on.");

        verbose_label.Location = new Point(0,40);
        verbose_label.Height = 20;
        verbose_label.AutoSize = false;
        verbose_label.Width = leftColumnWith;
        verbose_label.Text = "Console output is verbose:";
        verbose_label.Parent = this;
        button_tooltip.SetToolTip(verbose_label, "Is the console output a little more elaborate?");

        verbose_text.Location = new Point(leftColumnWith,40);
        verbose_text.Height = 20;
        verbose_text.AutoSize = false;
        verbose_text.Width = rightColumnWith;
        verbose_text.Text = Printer.GetVerbose() ? "yes" : "no";
        verbose_text.Parent = this;
        button_tooltip.SetToolTip(verbose_text, "Is the console output a little more elaborate?");

        debug_label.Location = new Point(0,60);
        debug_label.Height = 20;
        debug_label.AutoSize = false;
        debug_label.Width = leftColumnWith;
        debug_label.Text = "Using debug output on console:";
        debug_label.Parent = this;
        button_tooltip.SetToolTip(debug_label, "Is the console output very detailed?");

        debug_text.Location = new Point(leftColumnWith,60);
        debug_text.Height = 20;
        debug_text.AutoSize = false;
        debug_text.Width = rightColumnWith;
        debug_text.Text = Printer.GetDebug() ? "yes" : "no";
        debug_text.Parent = this;
        button_tooltip.SetToolTip(debug_text, "Is the console output very detailed?");

        MasterServerList_label.Location = new Point(0,80);
        MasterServerList_label.Height = 20;
        MasterServerList_label.AutoSize = false;
        MasterServerList_label.Width = leftColumnWith;
        MasterServerList_label.Text = "Master server list:";
        MasterServerList_label.Parent = this;
        button_tooltip.SetToolTip(MasterServerList_label, "What other master servers are queried?");

        string masterServerListString = "";
        string[] masterServerListStrings = Masterserver.GetMasterServerSources();
        if (masterServerListStrings == null || masterServerListStrings.Length == 0) {
            masterServerListString = "No servers provided, feature inactive";
        }
        else if (masterServerListStrings.Length == 1) {
            masterServerListString = masterServerListStrings[0];
        }
        else {
            masterServerListString = String.Join("\n", masterServerListStrings);
        }
        this.masterServerList.Text = masterServerListString;
        this.masterServerList.Location = new Point (leftColumnWith + 1, 80);
        this.masterServerList.Width = rightColumnWith;
        this.masterServerList.Height = 178;
        this.masterServerList.AcceptsReturn = true;
        this.masterServerList.AcceptsTab = false;
        this.masterServerList.Multiline = true;
        this.masterServerList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        button_tooltip.SetToolTip(masterServerList, "What other master servers are queried?");
        masterServerList.Parent = this;

        interval_label.Location = new Point(0,260);
        interval_label.Height = 20;
        interval_label.AutoSize = false;
        interval_label.Width = leftColumnWith;
        interval_label.Text = "Master server query interval:";
        interval_label.Parent = this;
        button_tooltip.SetToolTip(interval_label, "Interval other master servers are queried.");

        interval_text.Location = new Point(leftColumnWith,260);
        interval_text.Height = 20;
        interval_text.AutoSize = false;
        interval_text.Width = rightColumnWith;
        if (Masterserver.GetMasterServerQueryInterval() > 0) {
            interval_text.Text = Masterserver.GetMasterServerQueryInterval().ToString() + " Seconds";
        }
        else {
            interval_text.Text = "Not applied (query only once at startup)";
        }
        interval_text.Parent = this;
        button_tooltip.SetToolTip(interval_text, "Interval other master servers are queried.");

    }

    private void CloseThis(object sender, EventArgs e) {
        origin.Show();
        this.Close();
    }
}
