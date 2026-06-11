### About JKA Crossplatform

This lets your game console version of JKA (Switch, PS4) connect to a PC game server. 

The PC game server must be using a special build of openjkded and running a C# script to send a special matchmaking response packet.

### Requirements

- Windows PC that has Administrative access
- .NET 10 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- DNS server software like Technitium: https://technitium.com/dns/
- Compiled `openjkded.x86.exe` from this fork: https://github.com/erfg12/OpenJK-consoles

### How-To Use

1. Install .NET 10, and Technitium.
2. Open firewall ports `53` (TCP & UDP), `80` (TCP) and `29070` (UDP)
3. Set up Technitium (or another DNS software) - see below
4. Set up your game console - see below
5. Edit the `matchmaking.cs` script's `targetIpAddress` and `targetPort` variables to match your game server's IP:Port
6. Run `matchmaking.bat` file.
7. Run `openjkded.x86.exe` as you normally would host a JKA dedicated PC game server
8. Now you can player your JKA console game on a PC game server by selecting `Multiplayer` > `Play` > `Matchmaking` and selecting the game type that matches the PC game server

### Setting Up Technitium (or another DNS software)
1. Open Technitium
2. Click the Zone tab
3. Add a new zone with the name `gateway.sw-jkja-mp.eks.aspyr.com`
4. Click edit zone, click Add Record button
5. Leave everything default except change IPv4 Address should be your server hosting the JKA game server

### Setting Up Your Game Console
1. Open the menu to set up your internet connection
2. Set your internet connection's DNS on your game console to the IP of the PC running the Technitium server for both DNS1 and DNS2.
