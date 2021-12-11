all : masterserver.exe gameservers.exe masterserver.zip
ifeq ($(OS),Windows_NT)
### This section has only been tested under MobaXterm and requires the installation of package "zip" (apt-get install zip)
masterserver.exe : src\\Masterserver.cs src\\Exceptions.cs src\\HeartbeatListener.cs src\\HelpWindow.cs src\\QueryStrings.cs src\\ServerEntry.cs src\\ServerList.cs src\\Parser.cs src\\NetworkBasics.cs src\\Player.cs src\\Gui.cs src\\StatusBox.cs src\\Printer.cs graphics/ef_logo_256.ico
	mkdir -p build
	mkdir -p build/graphics
	cp graphics/ef_logo_48.ico build/graphics/
	cp readme.html build/
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:build\\masterserver.exe src\\Masterserver.cs src\\ServerEntry.cs src\\ServerList.cs src\\Exceptions.cs src\\QueryStrings.cs src\\HeartbeatListener.cs src\\HelpWindow.cs src\\Player.cs src\\Parser.cs src\\NetworkBasics.cs src\\Printer.cs src\\Gui.cs src\\StatusBox.cs -win32icon:graphics/ef_logo_256.ico
gameservers.exe : src\\Gameservers.cs src\\Exceptions.cs src\\QueryStrings.cs src\\ServerEntry.cs src\\ServerList.cs src\\Parser.cs src\\NetworkBasics.cs src\\Player.cs src\\Printer.cs graphics/ef_logo_256.ico
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -out:build\\gameservers.exe src\\Gameservers.cs src\\Exceptions.cs src\\QueryStrings.cs src\\ServerEntry.cs src\\ServerList.cs src\\Parser.cs src\\NetworkBasics.cs src\\Player.cs src\\Printer.cs -win32icon:graphics/ef_logo_256.ico
clean:
	rm -f build/masterserver.exe
	rm -f build/gameservers.exe
	rm -f build/masterserver.zip
	rm -f build/readme.html
	rm -f build/graphics/ef_logo_48.ico
	rmdir build/graphics/
	rmdir build/
masterserver.zip : build/masterserver.exe build/graphics/ef_logo_48.ico build/readme.html
	cd build && zip -9 masterserver.zip masterserver.exe graphics/ef_logo_48.ico readme.html
else
### This should work under normal Linux. You might need to install the mono suite, e.g. package monocomplete under Debian derivatives
masterserver.exe : src/Masterserver.cs src/Exceptions.cs src/HeartbeatListener.cs src/HelpWindow.cs src/QueryStrings.cs src/ServerEntry.cs src/ServerList.cs src/Parser.cs src/NetworkBasics.cs src/Player.cs src/Gui.cs src/StatusBox.cs src/Printer.cs graphics/ef_logo_256.ico
	mkdir -p build
	mkdir -p build/graphics
	mcs -out:build/masterserver.exe src/Masterserver.cs src/Exceptions.cs src/HeartbeatListener.cs src/HelpWindow.cs src/QueryStrings.cs src/ServerEntry.cs src/ServerList.cs src/Parser.cs src/NetworkBasics.cs src/Player.cs src/Gui.cs src/StatusBox.cs src/Printer.cs "-pkg:dotnet" "-define:SERVER" "-lib:/usr/lib/mono/2.0" -win32icon:graphics/ef_logo_256.ico
	cp graphics/ef_logo_48.ico build/graphics/
	cp readme.html build/
gameservers.exe : src/Gameservers.cs src/Exceptions.cs src/QueryStrings.cs src/ServerEntry.cs src/ServerList.cs src/Parser.cs src/NetworkBasics.cs src/Player.cs src/Printer.cs graphics/ef_logo_256.ico
	mkdir -p build
	mkdir -p build/graphics
	mcs -out:build/gameservers.exe src/Gameservers.cs src/Exceptions.cs src/QueryStrings.cs src/ServerEntry.cs src/ServerList.cs src/Parser.cs src/NetworkBasics.cs src/Player.cs src/Printer.cs "-pkg:dotnet" "-lib:/usr/lib/mono/2.0" -win32icon:graphics/ef_logo_256.ico
	cp graphics/ef_logo_48.ico build/graphics/
	cp readme.html build/
clean:
	rm -f build/masterserver.exe build/gameservers.exe build/masterserver.zip build/readme.html build/graphics/ef_logo_48.ico
	rmdir build/graphics
	rmdir build
masterserver.zip : build/masterserver.exe build/graphics/ef_logo_48.ico build/readme.html
	cd build && rm -f masterserver.zip && zip -9 -r masterserver.zip masterserver.exe graphics/ef_logo_48.ico readme.html
endif
