echo off
cls
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe -out:masterserver.exe Masterserver.cs ServerEntry.cs ServerList.cs Exceptions.cs HelpWindow.cs QueryStrings.cs HeartbeatListener.cs Player.cs Parser.cs NetworkBasics.cs Printer.cs Gui.cs StatusBox.cs -win32icon:graphics/ef_logo_256.ico
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe -out:gameservers.exe Gameservers.cs Exceptions.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Printer.cs -win32icon:graphics/ef_logo_256.ico
tar.exe -a -c -f masterserver.zip masterserver.exe graphics/ef_logo_48.ico readme.html
pause
