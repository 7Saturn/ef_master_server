using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

public class Gui : Form
{
    private ListView serverListTable;
    private StatusBox statusBox;

    public Gui(string version)
    {
        Printer.DebugMessage("Creating main window...");
        string icon48path = "graphics/ef_logo_48.ico";
        if (File.Exists(icon48path)) {
            Printer.DebugMessage("Loading main window icon...");
            Icon cornerIcon = new Icon (icon48path);
            this.Icon = cornerIcon;
        }
        else {
            Printer.DebugMessage(icon48path + " is missing, but it should be delivered along with this program.");
        }

        this.Size = new Size(576,432);
        this.Text = "EF Masterserver Version " + version;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        serverListTable = new ListView();
        serverListTable.Bounds = new Rectangle(new Point(10,10), new Size(549,353));
        // Set the view to show details.
        serverListTable.View = View.Details;
        // Prevent the user from editing item text.
        serverListTable.LabelEdit = false;
        // Allow the user to rearrange columns.
        serverListTable.AllowColumnReorder = true;
        // Display no check boxes.
        serverListTable.CheckBoxes = false;
        // Select the item and subitems when selection is made.
        serverListTable.FullRowSelect = true;
        // Display grid lines.
        serverListTable.GridLines = true;
        serverListTable.MultiSelect = false;
        //Initializing the list
        Refresh(null, null);

        // Add the ListView to the control collection.
        this.Controls.Add(serverListTable);

        Button exit_button = new Button();
        exit_button.Text = "Exit";
        ToolTip button_tooltip = new ToolTip(); //Can be used multiple times
        button_tooltip.SetToolTip(this, "Here you can see a list of currently known servers."); //Window explains itself. ;-)
        button_tooltip.SetToolTip(exit_button, "Closes the Masterserver (ESC)");
        this.Controls.Add(exit_button);
        exit_button.Location = new Point(359, 375);
        exit_button.Parent = this;
        CancelButton = exit_button;
		exit_button.Click += new EventHandler (Shutdown); //Event (Button_Click)
        this.FormClosing += new FormClosingEventHandler(Shutdown);

        Button refresh_button = new Button();
        refresh_button.Text = "Refresh";
        button_tooltip.SetToolTip(refresh_button, "Refreshes the Masterserver list from memory (Enter)");
        this.Controls.Add(refresh_button);
        refresh_button.Location = new Point(142, 375);
        refresh_button.Parent = this;
        AcceptButton = refresh_button;
		refresh_button.Click += new EventHandler (Refresh); //Event (Button_Click)

        Button status_button = new Button();
        status_button.Text = "Status";
        button_tooltip.SetToolTip(status_button, "Shows current state and settings of the masterserver");
        status_button.Location = new Point(250, 375);
        status_button.Parent = this;
		status_button.Click += new EventHandler (ShowStatus); //Event (Button_Click)

        CenterToScreen();
    }

    private void Shutdown (object sender, EventArgs e)
    {
        Printer.DebugMessage("Shutdown was requested.");
        Environment.Exit(0);
    }

    private void Refresh (object sender, EventArgs e)
    {
        Printer.DebugMessage("Refresh of main window was requested.");
        serverListTable.Clear();
        List<ServerEntry> serverList = ServerList.get_list();
        foreach (ServerEntry serverEntry in serverList) {
            if (serverEntry.GetProtocol() != -1) {
                ListViewItem serverItem = ListItemFromStrings(serverEntry.GetAddress() + ":" + serverEntry.GetPort(),
                                                              serverEntry.GetProtocol().ToString(),
                                                              serverEntry.IsEmpty() ? "yes" : "no",
                                                              serverEntry.IsFull() ? "yes" : "no");
                serverListTable.Items.Add(serverItem);
                serverListTable.Sorting = SortOrder.Ascending;
            }
        }
        SetTableHeader();
    }

    private ListViewItem ListItemFromStrings(string serverAndPort,
                                             string protocol,
                                             string isEmpty,
                                             string isFull) {
        ListViewItem listElement = new ListViewItem(serverAndPort);
        listElement.SubItems.Add(protocol);
        listElement.SubItems.Add(isEmpty);
        listElement.SubItems.Add(isFull);
        return listElement;
    }

    private void SetTableHeader() {
        // Create columns for the items and subitems.
        // Width of -1 indicates auto-size for data columns.
        // Width of -2 indicates auto-size for data header.
        serverListTable.Columns.Add("Server", 150, HorizontalAlignment.Left);
        serverListTable.Columns.Add("Protocol", 60, HorizontalAlignment.Center);
        serverListTable.Columns.Add("Is Empty", 60, HorizontalAlignment.Center);
        serverListTable.Columns.Add("Is Full", 45, HorizontalAlignment.Center);
    }

    private void ShowStatus(object sender, EventArgs e) {
        Printer.DebugMessage("Status window was requested");
        statusBox = new StatusBox(this);
        statusBox.Owner = this;
        statusBox.Show();
        this.Hide();
    }
}
