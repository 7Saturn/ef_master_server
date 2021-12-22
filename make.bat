echo off
cls
echo *** Creating folders ***
if not exist build mkdir build
if not exist build\graphics mkdir build\graphics
echo *** Copying static files ***
copy graphics\ef_logo_48.ico build\graphics\
copy readme.html build\
echo *** Building ef_masterserver.exe... ***
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe -out:build\ef_masterserver.exe src\Masterserver.cs src\ServerEntry.cs src\ServerList.cs src\Exceptions.cs src\HelpWindow.cs src\QueryStrings.cs src\HeartbeatListener.cs src\Player.cs src\Parser.cs src\NetworkBasics.cs src\Printer.cs src\Gui.cs src\StatusBox.cs -win32icon:graphics/ef_logo_256.ico
echo *** Building gameservers.exe ***
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe -out:build\gameservers.exe src\Gameservers.cs src\Exceptions.cs src\QueryStrings.cs src\ServerEntry.cs src\ServerList.cs src\Parser.cs src\NetworkBasics.cs src\Player.cs src\Printer.cs -win32icon:graphics/ef_logo_256.ico
echo *** Entering build ***
chdir build\
echo *** Packing ef_masterserver.zip ***
tar.exe -a -c -f ef_masterserver.zip ef_masterserver.exe graphics\ef_logo_48.ico readme.html
echo *** Leaving build ***
chdir ..
pause
