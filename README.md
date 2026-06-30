### About JKA Crossplatform

This lets your game console version of JKA (Switch/Switch 2, PS4/PS5) connect to a PC game server.

Video for setup: https://youtu.be/hJBtyvQQGJk

<img width="1072" height="682" alt="image" src="https://github.com/user-attachments/assets/cebe1408-7b9f-4c54-bd01-1f4e308976c0" />

<img width="1072" height="682" alt="image" src="https://github.com/user-attachments/assets/53adfa1a-3d58-4300-a8e1-3fffee74dc34" />

<img width="1000" height="900" alt="20260618_210753" src="https://github.com/user-attachments/assets/4962ebc3-17e7-4503-9ab5-fa08c058ff09" />


### Requirements

- PC that has Administrative access
- .NET 8+ SDK: https://dotnet.microsoft.com/en-us/download/dotnet

### Setup & How-To Use

1. Run JKACrossplatform, refresh the server list, select a server, or type in an IP and Port of the game server you want to join.
2. Set up your game console - use the IP address of your PC running JKACrossplatform as your DNS1. _(if you close JKACrossplatform connection will fail_)
3. Now you can play your JKA console game on a PC game server by selecting `Multiplayer` > `Play` > `Matchmaking` and selecting the game type that matches the PC game server

### Troubleshooting
- Q) Not receiving connections from Game Console to JkaProtocolProxy. A) Open firewall ports `53` (TCP & UDP), `80` (TCP), `29070` (UDP), `30000` (UDP)
- Q) When joining a game server it times out. A) Did you select a server running a custom map or is full?