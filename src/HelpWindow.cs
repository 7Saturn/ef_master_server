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
        helpContent += @"

This program sets up a master server for the game »Star Trek: Voyager Elite Force«. The above options let you configure its behavior when started on the console. For details see the provided documentation. For looking up the currently active settings of this instance use the »Status« button in the main window.

© 2025 by Martin Wohlauer.

 * You may use this program at your own leisure.
 * It comes free of charge.
 * It comes without any warranty whatsoever and no guaranteed suitability for a specific purpose.
 * You may use this program only at your own risk.
 * The source code of this software should come along with it. If not, ask the source from where you got this program, to provide it.
 * If in doubt about the technical implication, such as security, stability or any other technical fitnes, consult the source code.
 * You may alter the source code at your own discretion. If you do so, you are not allowed to remove the information of the original author and his copy right declaration. But you are encouraged to add your own name if you contributed to the project.";
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
