### About JKA Crossplatform

This lets your game console version of JKA (Switch, PS4) connect to a PC game server.

The PC game server must be using a special build of openjkded and running a C# script to send a special matchmaking response packet.

### Requirements

- PC that has Administrative access
- .NET 10 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- DNS server software like Technitium: https://technitium.com/dns/ or NAMO: https://www.mamp.info
- Python Arpspoof: https://pypi.org/project/ArpSpoof/ (or another MITM method)

### How-To Use

1. Install .NET 10, and Technitium.
2. Open firewall ports `53` (TCP & UDP), `80` (TCP), `29070` (UDP), `30000` (UDP)
3. Set up Technitium / NAMO - see below
4. Set up your game console - see below
5. Edit the `Program.cs` variables to match your game server's IP:Port and your ipconfig/ifconfig IP.
6. Run MacOS:`sudo dotnet run` or Windows:`dotnet run` in cmd with Administrative privileges.
7. Run python arpspoof with `arpspoof (GAME CONSOLE IP) (GATEWAY IP) -s -t 1`
8. Now you can player your JKA console game on a PC game server by selecting `Multiplayer` > `Play` > `Matchmaking` and selecting the game type that matches the PC game server

### Setting Up Your Game Console
1. Open the menu to set up your internet connection
2. Set your internet connection's DNS on your game console to the IP of the PC running the Technitium server for both DNS1 and DNS2.
3. Go back to your Internet settings option, note your game console's IP for later steps.

### Setting Up DNS Server (Technitium / NAMO)
1. Open Technitium or NAMO
2. Click the Zone tab
3. Add a new zone with the name `gateway.sw-jkja-mp.eks.aspyr.com`
4. Click edit zone, click Add Record button
5. Leave everything default except change IPv4 Address should be your server hosting the JKA game server
