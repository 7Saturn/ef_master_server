all : ef_masterserver.exe ef_masterserver.zip
ifeq ($(OS),Windows_NT)
### This section has only been tested under MobaXterm and requires the installation of package "zip" (apt-get install zip)
ef_masterserver.exe : src\\Masterserver.cs src\\Exceptions.cs src\\HeartbeatListener.cs src\\HelpWindow.cs src\\QueryStrings.cs src\\ServerEntry.cs src\\ServerList.cs src\\Parser.cs src\\NetworkBasics.cs src\\Player.cs src\\Gui.cs src\\StatusBox.cs src\\Printer.cs graphics/ef_logo_256.ico
	mkdir -p build
	mkdir -p build/graphics
	cp graphics/ef_logo_48.ico build/graphics/
	cp readme.html build/
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -warn:4 -define:SERVER -out:build\\ef_masterserver.exe src\\Masterserver.cs src\\ServerEntry.cs src\\ServerList.cs src\\Exceptions.cs src\\QueryStrings.cs src\\HeartbeatListener.cs src\\HelpWindow.cs src\\Player.cs src\\Parser.cs src\\NetworkBasics.cs src\\Printer.cs src\\Gui.cs src\\StatusBox.cs -win32icon:graphics/ef_logo_256.ico
gameservers.exe : src\\Gameservers.cs src\\Exceptions.cs src\\QueryStrings.cs src\\ServerEntry.cs src\\ServerList.cs src\\Parser.cs src\\NetworkBasics.cs src\\Player.cs src\\Printer.cs graphics/ef_logo_256.ico
	C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe -warn:4 -out:build\\gameservers.exe src\\Gameservers.cs src\\Exceptions.cs src\\QueryStrings.cs src\\ServerEntry.cs src\\ServerList.cs src\\Parser.cs src\\NetworkBasics.cs src\\Player.cs src\\Printer.cs -win32icon:graphics/ef_logo_256.ico
clean:
	rm -f build/ef_masterserver.exe
	rm -f build/gameservers.exe
	rm -f build/ef_masterserver.zip
	rm -f build/readme.html
	rm -f build/graphics/ef_logo_48.ico
	rmdir build/graphics/
	rmdir build/
ef_masterserver.zip : build/ef_masterserver.exe build/graphics/ef_logo_48.ico build/readme.html
	cd build && zip -9 ef_masterserver.zip ef_masterserver.exe graphics/ef_logo_48.ico readme.html
else
### This should work under normal Linux as well as WSL. You might need to install the mono suite, e.g. package mono-complete under Debian derivatives
ef_masterserver.exe : src/Masterserver.cs src/Exceptions.cs src/HeartbeatListener.cs src/HelpWindow.cs src/QueryStrings.cs src/ServerEntry.cs src/ServerList.cs src/Parser.cs src/NetworkBasics.cs src/Player.cs src/Gui.cs src/StatusBox.cs src/Printer.cs graphics/ef_logo_256.ico
	mkdir -p build
	mkdir -p build/graphics
	mcs -out:build/ef_masterserver.exe src/Masterserver.cs src/Exceptions.cs src/HeartbeatListener.cs src/HelpWindow.cs src/QueryStrings.cs src/ServerEntry.cs src/ServerList.cs src/Parser.cs src/NetworkBasics.cs src/Player.cs src/Gui.cs src/StatusBox.cs src/Printer.cs "-pkg:dotnet" "-define:SERVER" "-lib:/usr/lib/mono/4.8-api" -win32icon:graphics/ef_logo_256.ico
	cp graphics/ef_logo_48.ico build/graphics/
	cp readme.html build/
gameservers.exe : src/Gameservers.cs src/Exceptions.cs src/QueryStrings.cs src/ServerEntry.cs src/ServerList.cs src/Parser.cs src/NetworkBasics.cs src/Player.cs src/Printer.cs graphics/ef_logo_256.ico
	mkdir -p build
	mkdir -p build/graphics
	mcs -out:build/gameservers.exe src/Gameservers.cs src/Exceptions.cs src/QueryStrings.cs src/ServerEntry.cs src/ServerList.cs src/Parser.cs src/NetworkBasics.cs src/Player.cs src/Printer.cs "-pkg:dotnet" "-lib:/usr/lib/mono/4.8-api" -win32icon:graphics/ef_logo_256.ico
	cp graphics/ef_logo_48.ico build/graphics/
	cp readme.html build/
clean:
	rm -f build/ef_masterserver.exe build/gameservers.exe build/ef_masterserver.zip build/readme.html build/graphics/ef_logo_48.ico
	rmdir build/graphics
	rmdir build
ef_masterserver.zip : build/ef_masterserver.exe build/graphics/ef_logo_48.ico build/readme.html
	cd build && rm -f ef_masterserver.zip && zip -9 -r ef_masterserver.zip ef_masterserver.exe graphics/ef_logo_48.ico readme.html
endif
