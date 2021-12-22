echo off
cls
echo *** Removing created files ***
del /F build\ef_masterserver.exe build\ef_masterserver.zip build\gameservers.exe build\graphics\ef_logo_48.ico build\readme.html
echo *** Removing created folders ***
rmdir build\graphics
rmdir build
pause
