all:
	make masterserver gameservers
masterserver:
	mcs -out:masterserver.exe Masterserver.cs Exceptions.cs HeartbeatListener.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Gui.cs Printer.cs "-pkg:dotnet" "-lib:/usr/lib/mono/2.0" -win32icon:graphics/ef_logo_256.ico

gameservers:
	mcs -out:gameservers.exe Gameservers.cs Exceptions.cs HeartbeatListener.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Gui.cs Printer.cs "-pkg:dotnet" "-lib:/usr/lib/mono/2.0" -win32icon:graphics/ef_logo_256.ico
clean:
	rm -f masterserver.exe gameservers.exe
