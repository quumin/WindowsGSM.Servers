using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using System.Linq;
using System.Net;

namespace WindowsGSM.Plugins
{
    public class Satisfactory : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.Satisfactory",                                       // Plugin Name, of format WindowsGSM.XXXX
            author = "Q-min",                                                       // Plugin Author
            description = "Plugin for Satisfactory (Dedicated Server).",            // Plugin Description
            version = "1.1",                                                        /* Version History:
                                                                                     *  Version 1.1 -
                                                                                     *      Contains updates to comments, handling MaxPlayers, removal of -log flag so it can run headless.
                                                                                     *  Version 1.0 - 
                                                                                     *      Can be found on https://github.com/AIMI-SAYO/WindowsGSM.Satisfactory/tree/main.
                                                                                     */
            url = "https://github.com/quumin/WindowsGSM.Satisfactory/tree/main",    // Github Repository Link
            color = "#f9b234"                                                       // Color Hex, Lightning Yellow
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;                                // Login Anonymously to SteamCMD
        public override string AppId => "1690800";                                  // Satisfactory game server App ID on Steam

        // - Standard Constructor and properties
        public Satisfactory(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;

        public new string Error, Notice;                                            // Use 'new' keyword to hide inherited member

        // - Game server Fixed variables
        public override string StartPath =>
            @"Engine\Binaries\Win64\FactoryServer-Win64-Shipping-Cmd.exe";          // Game server start path
        public string FullName = "Satisfactory Dedicated Server";                   // Game server FullName
        public bool AllowsEmbedConsole = true;                                      // Does this server support output redirect?
        public int PortIncrements = 1;                                              // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S();                                      // Assign A2S query method or set to 'null'

        // - Game server default values
        public string Port = "7777";                                                // Default port
        public string QueryPort = "7777";                                           // Default query port, 15777 is no longer used as of 1.0.
                                                                                    //  See https://satisfactory.wiki.gg/wiki/Dedicated_servers#Port_Forwarding_and_Firewall_Settings.
        public string Defaultmap = "Dedicated";                                     // Placeholder default map, should be "Dedicated" to get Satisfactory to work.
        public string Maxplayers = "4";                                             // Default max players value
        public string Additional = "";                                              // Additional server start parameter

        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            await Task.Run(() =>
            {
                // No config file seems necessary
            });
        }//CreateServerCFG()

        // - Update the Game.ini file to change the MaxPlayers setting
        private void UpdateMaxPlayersInGameIni()
        {
            string gameIniPath = Path.Combine(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID), @"FactoryGame\Saved\Config\WindowsServer\Game.ini");
            if (File.Exists(gameIniPath))
            {
                var iniContent = File.ReadAllText(gameIniPath);

                //This is to add (or replace) the MaxPlayers setting
                //  Sometimes the max players won't update unless you also specify the tag.
                string maxPlayersParam = "MaxPlayers=";
                string maxPlayersTag = $"[/Script/Engine.GameSession]";
                string maxPlayersLine = $"{maxPlayersParam}{_serverData.ServerMaxPlayer}";
                //  If there's already a MaxPlayers field...
                if (iniContent.Contains(maxPlayersParam))
                {
                    //... and if the MaxPlayers Tag is not present...
                    if (!iniContent.Contains(maxPlayersTag))
                    {
                        iniContent = System.Text.RegularExpressions.Regex.Replace(iniContent, @$"{maxPlayersParam}\d+", $"{maxPlayersTag}\n\n{maxPlayersLine}");
                    }//if
                    else
                    {
                        iniContent = System.Text.RegularExpressions.Regex.Replace(iniContent, @$"{maxPlayersParam}\d+", maxPlayersLine);
                    }//else
                }//if
                else
                {
                    iniContent += $"{maxPlayersTag}\n\n{maxPlayersLine}";
                }//else

                File.WriteAllText(gameIniPath, iniContent);
            }//if
            else
            {
                Error = "Game.ini file not found!";
            }//else
        }//UpdateMaxPlayersInGameIni()

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string shipExePath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found (\'{shipExePath}\')";
                return null;
            }//if

            // Update the Game.ini file with the new MaxPlayers setting
            UpdateMaxPlayersInGameIni();

            // Prepare start parameter
            string param = "FactoryGame -unattended";
            param += $" {_serverData.ServerParam}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -Port={_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? string.Empty : $" -MaxPlayers={_serverData.ServerMaxPlayer}";

            // Prepare Process
            Process gameServerProcess = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                },
                EnableRaisingEvents = true
            };

            // If WindowsGSM "EmbedConsole" is enabled...
            if (AllowsEmbedConsole)
            {
                //... redirect output.
                gameServerProcess.StartInfo.RedirectStandardInput = true;
                gameServerProcess.StartInfo.RedirectStandardOutput = true;
                gameServerProcess.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                gameServerProcess.OutputDataReceived += serverConsole.AddOutput;
                gameServerProcess.ErrorDataReceived += serverConsole.AddOutput;

                //... start the Game Server Process.
                try
                {
                    gameServerProcess.Start();
                }//try
                catch (FileNotFoundException e)
                {
                    Error = $"\'FactoryServer.exe\' file not found: {e.Message}";
                    return null;
                }//catch
                catch (UnauthorizedAccessException e)
                {
                    Error = $"Access to \'FactoryServer.exe\' denied: {e.Message}";
                    return null;
                }//catch
                catch (Exception e)
                {
                    Error = $"Unknown exception with \'FactoryServer.exe\': {e.Message}";
                    return null;
                }//catch

                //... capture output to WindowsGSM.
                gameServerProcess.BeginOutputReadLine();
                gameServerProcess.BeginErrorReadLine();
            }//if
            else
            {
                //... start the Game Server Process.
                try
                {
                    gameServerProcess.Start();
                }//try
                catch (FileNotFoundException e)
                {
                    Error = $"\'FactoryServer.exe\' file not found: {e.Message}";
                    return null;
                }//catch
                catch (UnauthorizedAccessException e)
                {
                    Error = $"Access to \'FactoryServer.exe\' denied: {e.Message}";
                    return null;
                }//catch
                catch (Exception e)
                {
                    Error = $"Unknown exception with \'FactoryServer.exe\': {e.Message}";
                    return null;
                }//catch
            }//else
            return gameServerProcess;
        }//Start()

        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("^c");
            });
            await Task.Delay(20000); // Give time to shut down properly
        }//Stop()

        // fixes WinGSM bug, https://github.com/WindowsGSM/WindowsGSM/issues/57#issuecomment-983924499
        public new async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;
            await Task.Run(() => { p.WaitForExit(); });
            return p;
        }//Update()
    }//class
}//namespace
