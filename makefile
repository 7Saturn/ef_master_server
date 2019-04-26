all:	
	mcs -out:masterserver.exe Masterserver.cs Exceptions.cs HeartbeatListener.cs QueryStrings.cs ServerEntry.cs ServerList.cs "-pkg:dotnet" "-lib:/usr/lib/mono/2.0"
