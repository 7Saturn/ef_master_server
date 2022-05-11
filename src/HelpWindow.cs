using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class HelpWindow : Form
{
    private Gui origin;

    public HelpWindow(Gui sourceWindow)
    {
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

        ToolTip button_tooltip = new ToolTip();
        button_tooltip.SetToolTip(this, "This may help in using the master server.");

        Button close_button = new Button();
        close_button.Text = "Close";
        this.Controls.Add(close_button);
        close_button.Parent = this;
        CancelButton = close_button;
        close_button.Click += new EventHandler (CloseThis);
        button_tooltip.SetToolTip(close_button, "Closes this window and shows server list (ESC/Enter).");
        Gui.CenterButton(close_button);
        Gui.BottomButton(close_button);

        TextBox helpText = new TextBox();
        helpText.Location = new Point(0,0);
        helpText.Height = 360;
        helpText.Width = 570;
        helpText.Multiline = true;
        helpText.ScrollBars = ScrollBars.Vertical;
        helpText.ReadOnly = true;
        helpText.AutoSize = false;
        helpText.Font = new Font(FontFamily.GenericMonospace, helpText.Font.Size);
        helpText.Text = Masterserver.consoleHelpText;
        helpText.Parent = this;
        button_tooltip.SetToolTip(helpText, "Some explanations about this tool.");
    }

    private void CloseThis(object sender, EventArgs e) {
        Printer.DebugMessage("Showing main window...");
        origin.Show();
        Printer.DebugMessage("Closing help window...");
        this.Close();
    }
}
