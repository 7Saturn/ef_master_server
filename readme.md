# EF Master Server

## Summary
This repo provides a master server for the game Star Trek: Voyager Elite Force. It is written in C# and should be easily buildable on MacOS, Linux, FreeBSD and Windows, and be used afterwards on all of these systems interchangeable.

## Notice on Github Releases

Right now, when setting up a compiling action on Github, the (currently) latest Mono version 6.12.0.122 is used. This Mono version has a bug that will lead to binaries compiled by it, that will not work properly on Windows, when using a certain methog. EF Master Server is affected by this. So builds made by Github will not work properly on Windows. If you want to use it on Windows, build it yourself with the *make.bat*.

## Additional Information
For further details, see *readme.html* inside the repo. (It is intended to be shipped along with the build package and be viewed in a web browser.) I really don't believe in Mark Down to be used for this kind of work...
