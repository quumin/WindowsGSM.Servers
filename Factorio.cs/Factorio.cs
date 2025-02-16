using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    public class Factorio : SteamCMDAgent// SteamCMDAgent is used because factorio relies on SteamCMD for installation and update process
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.Factorio",                                       // Plugin Name, of format WindowsGSM.XXXX
            author = "Q-min",                                                   // Plugin Author
            description = "Plugin for Factorio (Dedicated Server).",            // Plugin Description
            version = "1.1",                                                        /* Version History:
                                                                                     *  Version 1.1 -
                                                                                     *      Contains updates to comments, handling MaxPlayers, removal of -log flag so it can run headless.
                                                                                     *  Version 1.0 - 
                                                                                     *      Can be found on https://github.com/Kickbut101/WindowsGSM.Factorio.
                                                                                     */
            url = "https://github.com/quumin/WindowsGSM.Servers",               // Github Repository Link
            color = "#f9b234"                                                   // Color Hex, Lightning Yellow
        };

        // - Standard Constructor and Properties:
        public Factorio(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        // Store server start metadata, such as start ip, port, start param, etc
        private readonly ServerConfig _serverData; 

        // - Settings Properties for SteamCMD Installer:
        //  Factorio Requires a Login to Install via SteamCMD, you will need to verify your account via SteamGuard.
        public override bool loginAnonymous => false;
        //  Factorio Game App ID for Steam.
        public override string AppId => "427520"; 

        // - Settings for the Actuya
        public string FullName = "Factorio Dedicated Server";
        public override string StartPath => @"bin\x64\factorio.exe";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()

        public string Port = "34197"; // Default factorio port - can be changed in config file - UDP only
        public string QueryPort = "27001"; // Unsure so far
        public string Defaultmap = "Default"; // Up to user, doesn't matter what's here.
        public string Maxplayers = "10";
        public string Additional = "";

        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            await Task.Run(() =>
            {
                // No config file seems necessary
            });
        }//CreateServerCFG()

        // - Start server function, return its Process to WindowsGSM    
        public async Task<Process> Start()
        {
            string cfgPath = @"..\Factorio\config\config.ini";
            string shipExePath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            string settingsPath = @".\data\server-settings.json";

            // Does .exe path exist?
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found in ({shipExePath})";
                return null;
            }

            // Prepare start parameters
            // This assumes you have already created a map, version to create a new map will be updated later.
            string param = $" --start-server"; // starting parameter for using the factorio.exe as a server
            param += string.IsNullOrWhiteSpace(_serverData.ServerMap) ? $" --create saves\\my-save.zip" : $"-load-latest saves\\{_serverData.ServerMap}.zip";
            param += $" --config={cfgPath}";
            param += $" --server-settings {settingsPath}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" --port {_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerParam) ? string.Empty : $" {_serverData.ServerParam}";

            // Prepare Process
            var gameServerProcess = new Process
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
                    Error = $"\'Factorio.exe\' file not found: {e.Message}";
                    return null;
                }//catch
                catch (UnauthorizedAccessException e)
                {
                    Error = $"Access to \'Factorio.exe\' denied: {e.Message}";
                    return null;
                }//catch
                catch (Exception e)
                {
                    Error = $"Unknown exception with \'Factorio.exe\': {e.Message}";
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
                    Error = $"\'Factorio.exe\' file not found: {e.Message}";
                    return null;
                }//catch
                catch (UnauthorizedAccessException e)
                {
                    Error = $"Access to \'Factorio.exe\' denied: {e.Message}";
                    return null;
                }//catch
                catch (Exception e)
                {
                    Error = $"Unknown exception with \'Factorio.exe\': {e.Message}";
                    return null;
                }//catch
            }//else
            await Task.Delay(10000);
            return gameServerProcess;
        }//Start()

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("^c"); // Send Ctrl+C command
                p.WaitForExit(5000);
            });
            await Task.Delay(500); // Give time to shut down properly
        }

        public new async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;
            await Task.Run(() => { p.WaitForExit(); });
            return p;
        }

        public new bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public new bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "PackageInfo.bin");
            Error = $"Invalid Path! Fail to find {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        public new string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public new async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }//class
}//namespace