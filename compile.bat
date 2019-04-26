echo off
cls
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe -out:masterserver.exe Masterserver.cs ServerEntry.cs ServerList.cs Exceptions.cs QueryStrings.cs HeartbeatListener.cs
rem pause
