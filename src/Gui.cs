using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

public class Gui : Form {
    private ListView serverListTable;
    private StatusBox statusBox;
    private HelpWindow helpBox;

    public delegate void DoRefreshFromOutside();

    public Gui(string version) {
        Printer.DebugMessage("Creating main window...");
        string icon48path = "graphics/ef_logo_48.ico";
        if (File.Exists(icon48path)) {
            Printer.DebugMessage("Loading main window icon...");
            Icon cornerIcon = new Icon (icon48path);
            this.Icon = cornerIcon;
        }
        else {
            Printer.DebugMessage(icon48path + " is missing, but it should be"
                                 + " delivered along with this program.");
        }

        this.Size = new Size(576,432);
        this.Text = "EF Masterserver Version " + version;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.KeyDown += HandleMainKeys;
        this.KeyPreview = true;

        DoRefresh();

        Button exitButton = new Button();
        exitButton.Text = "Exit";
        ToolTip buttonTooltip = new ToolTip(); //Can be used multiple times

        Button helpButton = new Button();
        helpButton.Text = "?";
        buttonTooltip.SetToolTip(helpButton,
                                 "Shows some help for the masterserver (F1)");
        helpButton.Location = new Point(135, 0);
        helpButton.Width=20;
        helpButton.Parent = this;
        BottomButton(helpButton);
        helpButton.Click += new EventHandler (ShowHelp);

        Button refreshButton = new Button();
        refreshButton.Text = "Refresh";
        buttonTooltip.SetToolTip(refreshButton,
                                 "Refreshes the Masterserver list from memory"
                                 + " (F5)");
        this.Controls.Add(refreshButton);
        refreshButton.Location = new Point(172, 0);
        refreshButton.Parent = this;
        refreshButton.Click += new EventHandler (Refresh);
        BottomButton(refreshButton);

        Button statusButton = new Button();
        statusButton.Text = "Status";
        buttonTooltip.SetToolTip(statusButton,
                                 "Shows current state and settings of the"
                                 + " masterserver (F6)");
        statusButton.Location = new Point(265, 0);
        statusButton.Parent = this;
        statusButton.Click += new EventHandler (ShowStatus);
        AcceptButton = statusButton;
        BottomButton(statusButton);

        buttonTooltip.SetToolTip(exitButton,
                                 "Closes the Masterserver (F7/ESC)");
        this.Controls.Add(exitButton);
        exitButton.Location = new Point(359, 0);
        exitButton.Parent = this;
        CancelButton = exitButton;
        exitButton.Click += new EventHandler (Shutdown);
        this.FormClosing += new FormClosingEventHandler(Shutdown);
        BottomButton(exitButton);

        CenterToScreen();
        #if SERVER
            ServerList.RegisterObserver(this);
        #endif
    }

    private void Shutdown(object sender, EventArgs e) {
        Printer.DebugMessage("Shutdown was requested.");
        if (Masterserver.GetOtherMasterServerQueryThread() != null) {
            Printer.DebugMessage("Aborting query thread...");
            Masterserver.GetOtherMasterServerQueryThread().Abort();
            Printer.DebugMessage("Query thread aborted.");
        }
        Printer.DebugMessage("Exiting...");
        Environment.Exit(0);
    }

    private void Refresh(object sender, EventArgs e) {
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
        ToolTip buttonTooltip = new ToolTip(); //Can be used multiple times
        buttonTooltip.SetToolTip(tempList,
                                 "List of known servers. Click on an entry and"
                                 + " press CTRL + C to copy its address and"
                                 + " port.");

        tempList.KeyDown += CopyThat;

        tempList.BeginUpdate();
        InitalizeServerListTable(ref tempList);
        Printer.DebugMessage("Building List...");
        List<ServerEntry> serverList = ServerList.GetList();
        lock(serverList) {
            serverList.Sort((x, y) => x.serverEntryinHex().CompareTo(
                                y.serverEntryinHex()));
            int counter = 0;
            foreach (ServerEntry serverEntry in serverList) {
                if (serverEntry.GetProtocol() != -1) {
                    Printer.DebugMessage("Adding List Items");
                    string gameServerAddress = serverEntry.GetAddressString();
                    if (serverEntry.IsIpV6()) {
                        gameServerAddress = "[" + gameServerAddress + "]";
                    }
                    counter++;
                    ListViewItem serverItem = ListItemFromStrings(
                        counter,
                        gameServerAddress
                        + ":"
                        + serverEntry.GetPort(),
                        serverEntry.GetHostname(),
                        serverEntry.GetProtocol().ToString(),
                        serverEntry.IsEmpty() ? "yes"
                        : "no",
                        serverEntry.IsFull() ? "yes"
                        : "no");
                    tempList.Items.Add(serverItem);
                }
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
        if (serverListTable.InvokeRequired) {
            Printer.DebugMessage("invoke");
            var d = new DoRefreshFromOutside(DoRefresh);
            Printer.DebugMessage("invoking...");
            serverListTable.Invoke(d);
        }
        else {
            Printer.DebugMessage("normal");
            Refresh();
        }
    }

    private ListViewItem ListItemFromStrings(int counter,
                                             string serverAndPort,
                                             string hostname,
                                             string protocol,
                                             string isEmpty,
                                             string isFull) {
        ListViewItem listElement = new ListViewItem(counter.ToString());
        listElement.SubItems.Add(serverAndPort);
        listElement.SubItems.Add(hostname);
        listElement.SubItems.Add(protocol);
        listElement.SubItems.Add(isEmpty);
        listElement.SubItems.Add(isFull);
        return listElement;
    }

    private void CopyThat(object sender, KeyEventArgs e) {
        if (e.Control && e.KeyCode == Keys.C) {
            Printer.DebugMessage("CTRL + C detected");
            Printer.DebugMessage("From the list!");
            ListViewItem markedOne = serverListTable.FocusedItem;
            if (markedOne != null) {
                string serverAddress = markedOne.SubItems[1].Text;
                Printer.DebugMessage("List row " + markedOne.ToString()
                                     + " address: " + serverAddress);
                Clipboard.SetText(serverAddress);
            }
        }
    }

    private void HandleMainKeys(object sender, KeyEventArgs e) {
        if (!e.Control && e.KeyCode == Keys.F5) {
            DoRefresh();
        }
        else if (   !e.Control
                 && (   e.KeyCode == Keys.Escape
                     || e.KeyCode == Keys.F7)) {
            Shutdown(sender, e);
        }
        else if (!e.Control && e.KeyCode == Keys.F6) {
            ShowStatus(sender, e);
        }
        else if (!e.Control && e.KeyCode == Keys.F1) {
            ShowHelp(sender, e);
        }
    }

    private void SetTableHeader(ref ListView listView) {
        Printer.DebugMessage("SetTableHeader");
        // Create columns for the items and subitems.
        // Width of -1 indicates auto-size for data columns.
        // Width of -2 indicates auto-size for data header.
        listView.Columns.Add("No.",              -2 /*150*/,
                             HorizontalAlignment.Left);
        listView.Columns.Add("Game Server",      -2 /*150*/,
                             HorizontalAlignment.Left);
        listView.Columns.Add("Game Server Name", -2 /*214*/,
                             HorizontalAlignment.Left);
        listView.Columns.Add("Protocol",         -2 /* 60*/,
                             HorizontalAlignment.Center);
        listView.Columns.Add("Is Empty",         -2 /* 60*/,
                             HorizontalAlignment.Center);
        listView.Columns.Add("Is Full",          -2 /* 45*/,
                             HorizontalAlignment.Center);
    }

    private void ShowStatus(object sender, EventArgs e) {
        Printer.DebugMessage("Status window was requested");
        statusBox = new StatusBox(this);
        statusBox.Owner = this;
        statusBox.Show();
        this.Hide();
    }

    private void ShowHelp(object sender, EventArgs e) {
        Printer.DebugMessage("Help window was requested");
        helpBox = new HelpWindow(this);
        helpBox.Owner = this;
        helpBox.Show();
        this.Hide();
    }

    private void InitalizeServerListTable(ref ListView newListView) {
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

    public static void CenterButton(Button button) {
        int newX = (button.Parent.Width - button.Width) / 2;
        Point currentLocation = button.Location;
        currentLocation.X = newX;
        button.Location = currentLocation;
    }

    public static void BottomButton(Button button) {
        int newY = button.Parent.Height - button.Height - 34;
        Point currentLocation = button.Location;
        currentLocation.Y = newY;
        button.Location = currentLocation;
    }

}
