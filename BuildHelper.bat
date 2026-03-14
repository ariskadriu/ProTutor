@echo off
echo PREPARING NEW VERSION...
echo 1. Cleaning old build...
if exist publish rmdir /s /q publish
echo 2. Building standalone app...
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
echo.
echo DONE! 
echo Your new version is in the 'publish' folder.
echo Zip that folder and send it to your students!
pause
