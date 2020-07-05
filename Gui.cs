using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

public class Gui : Form
{
    private ListView serverListTable;
    private StatusBox statusBox;

    public delegate void DoRefreshFromOutside();

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
        this.KeyDown += HandleMainKeys;
        this.KeyPreview = true;

        DoRefresh();

        Button exit_button = new Button();
        exit_button.Text = "Exit";
        ToolTip button_tooltip = new ToolTip(); //Can be used multiple times

        Button refresh_button = new Button();
        refresh_button.Text = "Refresh";
        button_tooltip.SetToolTip(refresh_button, "Refreshes the Masterserver list from memory (F5)");
        this.Controls.Add(refresh_button);
        refresh_button.Location = new Point(142, 375);
        refresh_button.Parent = this;
		refresh_button.Click += new EventHandler (Refresh); //Event (Button_Click)

        Button status_button = new Button();
        status_button.Text = "Status";
        button_tooltip.SetToolTip(status_button, "Shows current state and settings of the masterserver (F6)");
        status_button.Location = new Point(250, 375);
        status_button.Parent = this;
		status_button.Click += new EventHandler (ShowStatus); //Event (Button_Click)
        AcceptButton = status_button;

        button_tooltip.SetToolTip(exit_button, "Closes the Masterserver (F7/ESC)");
        this.Controls.Add(exit_button);
        exit_button.Location = new Point(359, 375);
        exit_button.Parent = this;
        CancelButton = exit_button;
		exit_button.Click += new EventHandler (Shutdown); //Event (Button_Click)
        this.FormClosing += new FormClosingEventHandler(Shutdown);

        CenterToScreen();
        #if SERVER
            ServerList.RegisterObserver(this);
        #endif
    }

    private void Shutdown (object sender, EventArgs e)
    {
        Printer.DebugMessage("Shutdown was requested.");
        if (Masterserver.GetOtherMasterServerQueryThread() != null) {
            Masterserver.GetOtherMasterServerQueryThread().Abort();
        }
        Environment.Exit(0);
    }

    private void Refresh (object sender, EventArgs e)
    {
        Printer.DebugMessage("Refresh of main window was requested by button.");
        DoRefresh();
    }

    public void DoRefresh() {
        Printer.DebugMessage("Doing refresh of main window.");

        if (serverListTable != null) {
            Printer.DebugMessage("removing serverListTable");
            this.Controls.Remove(serverListTable);
        }
        ListView tempList = new ListView();
        ToolTip button_tooltip = new ToolTip(); //Can be used multiple times
        button_tooltip.SetToolTip(tempList, "List of known servers. Click on an entry and press CTRL + C to copy its address and port.");

        tempList.KeyDown += CopyThat;

        tempList.BeginUpdate();
        InitalizeServerListTable(ref tempList);
        Printer.DebugMessage("Building List...");
        List<ServerEntry> serverList = ServerList.get_list();
        foreach (ServerEntry serverEntry in serverList) {
            if (serverEntry.GetProtocol() != -1) {
                Printer.DebugMessage("Adding List Items");
                ListViewItem serverItem = ListItemFromStrings(serverEntry.GetAddress() + ":" + serverEntry.GetPort(),
                                                              serverEntry.GetHostname(),
                                                              serverEntry.GetProtocol().ToString(),
                                                              serverEntry.IsEmpty() ? "yes" : "no",
                                                              serverEntry.IsFull() ? "yes" : "no");
                tempList.Items.Add(serverItem);
                tempList.Sorting = SortOrder.Ascending;
            }
        }
        Printer.DebugMessage("Adding Header...");
        SetTableHeader(ref tempList);
        serverListTable = tempList;
        serverListTable.EndUpdate();
        this.Controls.Add(serverListTable);
        this.Focus();
    }

    public void RefreshSafe() {
        Printer.DebugMessage("RefreshSafe");
        if (serverListTable.InvokeRequired)
        {
            Printer.DebugMessage("invoke");
            var d = new DoRefreshFromOutside(DoRefresh);
            Printer.DebugMessage("invoking...");
            serverListTable.Invoke(d);
        }
        else
        {
            Printer.DebugMessage("normal");
            Refresh();
        }

    }

    private ListViewItem ListItemFromStrings(string serverAndPort,
                                             string hostname,
                                             string protocol,
                                             string isEmpty,
                                             string isFull) {
        ListViewItem listElement = new ListViewItem(serverAndPort);
        listElement.SubItems.Add(hostname);
        listElement.SubItems.Add(protocol);
        listElement.SubItems.Add(isEmpty);
        listElement.SubItems.Add(isFull);
        return listElement;
    }

    private void CopyThat(object sender, KeyEventArgs e) {
        if (e.Control && e.KeyCode == Keys.C) {
            Printer.DebugMessage("CTRL + C detected");
            Printer.DebugMessage("Von der Liste!");
            ListViewItem markedOne = serverListTable.FocusedItem;
            if (markedOne != null) {
                Printer.DebugMessage(markedOne.ToString());
                string serverAddress = markedOne.Text;
                Clipboard.SetText(serverAddress);
            }
        }
    }

    private void HandleMainKeys(object sender, KeyEventArgs e) {
        if (!e.Control && e.KeyCode == Keys.F5) {
            DoRefresh();
        }
        else if (!e.Control && ((e.KeyCode == Keys.Escape) || (e.KeyCode == Keys.F7))) {
            Shutdown(sender, e);
        }
        else if (!e.Control && e.KeyCode == Keys.F6) {
            ShowStatus(sender, e);
        }
    }


    private void SetTableHeader(ref ListView listView) {
        Printer.DebugMessage("SetTableHeader");
        // Create columns for the items and subitems.
        // Width of -1 indicates auto-size for data columns.
        // Width of -2 indicates auto-size for data header.
        listView.Columns.Add("Server", 150, HorizontalAlignment.Left);
        listView.Columns.Add("Host Name", 214, HorizontalAlignment.Left);
        listView.Columns.Add("Protocol", 60, HorizontalAlignment.Center);
        listView.Columns.Add("Is Empty", 60, HorizontalAlignment.Center);
        listView.Columns.Add("Is Full", 45, HorizontalAlignment.Center);
    }

    private void ShowStatus(object sender, EventArgs e) {
        Printer.DebugMessage("Status window was requested");
        statusBox = new StatusBox(this);
        statusBox.Owner = this;
        statusBox.Show();
        this.Hide();
    }

    private void InitalizeServerListTable (ref ListView newListView) {
        Printer.DebugMessage("InitalizeServerListTable");
        newListView.Bounds = new Rectangle(new Point(10,10), new Size(549,353));
        // Set the view to show details.
        newListView.View = View.Details;
        newListView.HideSelection = false;
        // Prevent the user from editing item text.
        newListView.LabelEdit = false;
        // Allow the user to rearrange columns.
        newListView.AllowColumnReorder = true;
        // Display no check boxes.
        newListView.CheckBoxes = false;
        // Select the item and subitems when selection is made.
        newListView.FullRowSelect = true;
        // Display grid lines.
        newListView.GridLines = true;
        newListView.MultiSelect = false;
    }

}
