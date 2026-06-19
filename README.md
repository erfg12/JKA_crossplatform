### About JKA Crossplatform

This lets your game console version of JKA (Switch/Switch 2, PS4/PS5) connect to a PC game server.

<img width="1072" height="682" alt="image" src="https://github.com/user-attachments/assets/cebe1408-7b9f-4c54-bd01-1f4e308976c0" />

<img width="1072" height="682" alt="image" src="https://github.com/user-attachments/assets/53adfa1a-3d58-4300-a8e1-3fffee74dc34" />

<img width="1000" height="900" alt="20260618_210753" src="https://github.com/user-attachments/assets/4962ebc3-17e7-4503-9ab5-fa08c058ff09" />


### Requirements

- PC that has Administrative access
- .NET 8+ SDK: https://dotnet.microsoft.com/en-us/download/dotnet
- DNS server software like Technitium (Windows): https://technitium.com/dns/ or NAMO (MacOS): https://www.mamp.info/namo/en/
- Python w/ Arpspoof installed: https://pypi.org/project/ArpSpoof/ (or another MITM method)

### Setup & How-To Use

1. Install .NET 8+, DNS server software and Python w/ Arpspoof.
2. Set up Technitium / NAMO - see below
3. Set up your game console - see below
4. Run python arpspoof with `arpspoof (GATEWAY IP) (GAME CONSOLE IP) -s -t 1` _(example: `arpspoof 192.168.0.1 192.168.0.99 -s -t 1`)_
5. Run JKACrossplatform, refresh the server list, select a server, or type in an IP and Port of the game server you want to join.
6. Now you can play your JKA console game on a PC game server by selecting `Multiplayer` > `Play` > `Matchmaking` and selecting the game type that matches the PC game server

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

### Troubleshooting
Not receiving connections from Game Console to JkaProtocolProxy? Open firewall ports `53` (TCP & UDP), `80` (TCP), `29070` (UDP), `30000` (UDP)
