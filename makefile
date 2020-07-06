all : masterserver.exe masterserver.zip gameservers.exe
ifeq ($(OS),Windows_NT)
masterserver.exe : Masterserver.cs Exceptions.cs HeartbeatListener.cs HelpWindow.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Gui.cs StatusBox.cs Printer.cs graphics/ef_logo_256.ico
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:masterserver.exe Masterserver.cs ServerEntry.cs ServerList.cs Exceptions.cs QueryStrings.cs HeartbeatListener.cs HelpWindow.cs Player.cs Parser.cs NetworkBasics.cs Printer.cs Gui.cs StatusBox.cs -win32icon:graphics/ef_logo_256.ico
gameservers.exe : Gameservers.cs Exceptions.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Printer.cs graphics/ef_logo_256.ico
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:gameservers.exe Gameservers.cs Exceptions.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Printer.cs -win32icon:graphics/ef_logo_256.ico
clean:
	if exist masterserver.exe del masterserver.exe
	if exist gameservers.exe del gameservers.exe
	if exist masterserver.zip del masterserver.zip
masterserver.zip : masterserver.exe graphics/ef_logo_48.ico
	tar -cf masterserver.zip masterserver.exe graphics/ef_logo_48.ico
else
masterserver.exe : Masterserver.cs Exceptions.cs HeartbeatListener.cs HelpWindow.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Gui.cs StatusBox.cs Printer.cs graphics/ef_logo_256.ico
	mcs -out:masterserver.exe Masterserver.cs Exceptions.cs HeartbeatListener.cs HelpWindow.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Gui.cs StatusBox.cs Printer.cs "-pkg:dotnet" "-define:SERVER" "-lib:/usr/lib/mono/2.0" -win32icon:graphics/ef_logo_256.ico
gameservers.exe : Gameservers.cs Exceptions.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Printer.cs graphics/ef_logo_256.ico
	mcs -out:gameservers.exe Gameservers.cs Exceptions.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Printer.cs "-pkg:dotnet" "-lib:/usr/lib/mono/2.0" -win32icon:graphics/ef_logo_256.ico
clean:
	rm -f masterserver.exe gameservers.exe masterserver.zip
masterserver.zip : masterserver.exe graphics/ef_logo_48.ico
	zip -9 masterserver.zip masterserver.exe graphics/ef_logo_48.ico
endif
