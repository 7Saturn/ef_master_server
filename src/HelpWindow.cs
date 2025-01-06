using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class HelpWindow : Form {
    private Gui origin;

    public HelpWindow(Gui sourceWindow) {
        if (sourceWindow.Icon != null) {
            this.Icon = sourceWindow.Icon;
        }
        this.origin = sourceWindow;
        Printer.DebugMessage("Creating help window...");
        this.Size = new Size(576,432);
        this.Text = "Help for EF Masterserver";
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.ShowInTaskbar = true;
        CenterToScreen();

        ToolTip buttonTooltip = new ToolTip();
        buttonTooltip.SetToolTip(this,
                                 "This may help in using the master server.");

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

        TextBox helpText = new TextBox();
        helpText.Location = new Point(0,0);
        helpText.Height = 360;
        helpText.Width = 570;
        helpText.Multiline = true;
        helpText.ScrollBars = ScrollBars.Vertical;
        helpText.ReadOnly = true;
        helpText.AutoSize = false;
        helpText.Font = new Font(FontFamily.GenericMonospace,
                                 helpText.Font.Size);
        string helpContent = Masterserver.consoleHelpText;
        string currentSystemType =
            System.Environment.OSVersion.Platform.ToString();
        if (currentSystemType.Equals("Win32NT")) {
            helpContent = Regex.Replace (helpContent, "\n", "\r\n");
        }
        helpText.Text = helpContent;
        helpText.Parent = this;
        buttonTooltip.SetToolTip(helpText,
                                 "Some explanations about this tool.");
    }

    private void CloseThis(object sender, EventArgs e) {
        Printer.DebugMessage("Showing main window...");
        origin.Show();
        Printer.DebugMessage("Closing help window...");
        this.Close();
    }
}
