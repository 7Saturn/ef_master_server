all : masterserver.exe gameservers.exe masterserver.zip
ifeq ($(OS),Windows_NT)
masterserver.exe : Masterserver.cs Exceptions.cs HeartbeatListener.cs HelpWindow.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Gui.cs StatusBox.cs Printer.cs graphics/ef_logo_256.ico
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:masterserver.exe Masterserver.cs ServerEntry.cs ServerList.cs Exceptions.cs QueryStrings.cs HeartbeatListener.cs HelpWindow.cs Player.cs Parser.cs NetworkBasics.cs Printer.cs Gui.cs StatusBox.cs -win32icon:graphics/ef_logo_256.ico
gameservers.exe : Gameservers.cs Exceptions.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Printer.cs graphics/ef_logo_256.ico
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:gameservers.exe Gameservers.cs Exceptions.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Printer.cs -win32icon:graphics/ef_logo_256.ico
clean:
	if exist masterserver.exe del masterserver.exe
	if exist gameservers.exe del gameservers.exe
	if exist masterserver.zip del masterserver.zip
masterserver.zip : masterserver.exe graphics/ef_logo_48.ico readme.html
	tar -cf masterserver.zip masterserver.exe graphics/ef_logo_48.ico readme.html
else
masterserver.exe : Masterserver.cs Exceptions.cs HeartbeatListener.cs HelpWindow.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Gui.cs StatusBox.cs Printer.cs graphics/ef_logo_256.ico
	mkdir -p build
	mkdir -p build/graphics
	mcs -out:build/masterserver.exe Masterserver.cs Exceptions.cs HeartbeatListener.cs HelpWindow.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Gui.cs StatusBox.cs Printer.cs "-pkg:dotnet" "-define:SERVER" "-lib:/usr/lib/mono/2.0" -win32icon:graphics/ef_logo_256.ico
	cp graphics/ef_logo_48.ico build/graphics/
	cp readme.html build/
gameservers.exe : Gameservers.cs Exceptions.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Printer.cs graphics/ef_logo_256.ico
	mkdir -p build
	mkdir -p build/graphics
	mcs -out:build/gameservers.exe Gameservers.cs Exceptions.cs QueryStrings.cs ServerEntry.cs ServerList.cs Parser.cs NetworkBasics.cs Player.cs Printer.cs "-pkg:dotnet" "-lib:/usr/lib/mono/2.0" -win32icon:graphics/ef_logo_256.ico
	cp graphics/ef_logo_48.ico build/graphics/
	cp readme.html build/
clean:
	rm -f build/masterserver.exe build/gameservers.exe build/masterserver.zip build/readme.html build/graphics/ef_logo_48.ico
	rmdir build/graphics
	rmdir build
masterserver.zip : build/masterserver.exe build/graphics/ef_logo_48.ico build/readme.html
	cd build && rm -f masterserver.zip && zip -9 -r masterserver.zip masterserver.exe graphics/ef_logo_48.ico readme.html
endif
