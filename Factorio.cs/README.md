## Factorio Requirements
You must also own the game Factorio! And you will need to use the "Set Account" feature in Windows GSM.

Note that this currently has a bug in it thanks to Factorio not idling, see (https://github.com/WindowsGSM/WindowsGSM/pull/201/commits/9e592e6cb226eda2f47827f31f358a8275cb4d13).

It needs to be fixed at a higher level in WindowsGSM, so I'll do that later.

## Installation
1. Move **Factorio.cs** folder to **plugins** folder
1. Click **[RELOAD PLUGINS]** button or restart WindowsGSM
1. You will still need to [make the map file manually](https://wiki.factorio.com/Multiplayer#Dedicated.2FHeadless_server) and place it in your serverfiles\bin\x64\ folder. Name the zip file as \<YourMapName\>_save.zip
1. Be sure to setup your server settings in the "server-settings.json" file in \servers\1\serverfiles\data directory

## Known issues
Factorio as a whole (not just with windowsGSM) doesn't seem to handoff the process ID. So when starting the server it will perpetually be in "Starting" status, instead of "Started". This also means server commands don't work correctly unless you DON'T check the embedconsole option, and you pipe in your commands to the window that pops up.


## Additional Command Line options
See all the possible commands [Here](https://wiki.factorio.com/Command_line_parameters). Here are some basic examples.


| Parameter | Description |
| --- | --- |
| --port N | Network port "N" to use. |
| -bind ADDRESS[:PORT] | IP address (and optionally port) to bind to. |
| --rcon-port N | Port "N" to use for RCON. |
| --rcon-bind ADDRESS:PORT | IP address and port to use for RCON. |
| --rcon-password PASSWORD | Password for RCON. |
| --server-settings FILE | Path to file with server settings. See data/server-settings.example.json.|
| --use-server-whitelist BOOL | If the whitelist should be used. |
| --server-whitelist FILE | Path to file with server whitelist. |
| --server-banlist FILE | Path to file with server banlist. |
| --server-adminlist FILE | Path to file with server adminlist. |
| --console-log FILE | Path to file where a copy of the server's log will be stored. |
| --server-id FILE | Path where server ID will be stored or read from. |