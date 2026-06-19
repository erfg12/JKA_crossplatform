cd ./installer
dotnet publish -c Release -r win-x86 -p:PublishSingleFile=true -p:SelfContained=false --no-self-contained
pause
