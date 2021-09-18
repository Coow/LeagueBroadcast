using Common;
using Common.Config;
using Common.Config.Files;
using LCUSharp;
using LCUSharp.Websocket;
using Server.Config;
using Server.PreGame.ChampSelect;
using Server.PreGame.ChampSelect.Data.LCU;
using Server.PreGame.ChampSelect.StateInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Utils;
using Utils.Log;

namespace Server.Data.Provider
{
    public static class ClientDataProvider
    {
        public static event EventHandler? GameLoad, GameStop, ChampSelectStart, ChampSelectStop, ClientConnected, ClientDisconnected;
        public static event EventHandler<Process>? GameStart;
        public static LeagueClientApi? API { get; set; }

        private static ComponentConfig? _componentCfg;

        private static ComponentConfig ComponentCfg
        {
            get
            {
                if (_componentCfg is null)
                {
                    _componentCfg = ConfigController.Get<ComponentConfig>();
                }

                return _componentCfg;
            }
        }

        private static Task? InitTask { get; set; }


        public static async Task<bool> InitConnectionWithinDuration(int timeoutInMS)
        {
            InitTask = InitConnection();
            return await Task.WhenAny(InitTask, Task.Delay(timeoutInMS)) == InitTask;
        }

        public static async Task InitConnection()
        {
            await ConnectToClient();
            API!.Disconnected += async (e, a) =>
            {
                "Client Disconnected! Attempting to reconnect...".Info();
                State.LeagueDisconnected();
                AppStatus.UpdateStatus(ConnectionStatus.Disconnected);
                ClientDisconnected?.Invoke(null, EventArgs.Empty);
                await API.ReconnectAsync();
                State.LeagueConntected();
                "Client Reconnected!".Info();
                ClientConnected?.Invoke(null, EventArgs.Empty);
            };
        }

        private static async Task ConnectToClient()
        {
            "Connecting to League Client".Info();
            Stopwatch stopwatch = Stopwatch.StartNew();
            API = await LeagueClientApi.ConnectAsync();
            API.EventHandler.Subscribe("/lol-gameflow/v1/gameflow-phase", ClientStateChanged);
            API.EventHandler.Subscribe("/lol-champ-select/v1/session", ChampSelectChanged);
            stopwatch.Stop();
            $"Connected to League Client in {stopwatch.ElapsedMilliseconds} ms".Info();
            State.LeagueConntected();

            ClientConnected?.Invoke(null, EventArgs.Empty);
        }

        private static void ClientStateChanged(object? sender, LeagueEvent e)
        {
            string eventType = e.Data.ToString();
            $"League State: {eventType}".Info();

            
            if (!eventType.Equals("InProgress", StringComparison.Ordinal) && AppStatus.CurrentStatus == ConnectionStatus.Ingame)
            {
                GameStop?.Invoke(null, EventArgs.Empty);
            }
            if (!eventType.Equals("ChampSelect", StringComparison.Ordinal) && AppStatus.CurrentStatus == ConnectionStatus.PreGame)
            {
                ChampSelectStop?.Invoke(null, EventArgs.Empty);
            }

            if (eventType.Equals("ChampSelect", StringComparison.Ordinal) && AppStatus.CurrentStatus != ConnectionStatus.PreGame)
            {
                ChampSelectStart?.Invoke(null, EventArgs.Empty);
            }
            
        }

        private static void ChampSelectChanged(object? sender, LeagueEvent e)
        {
            if (ConfigController.Get<ComponentConfig>().PickBan.IsActive)
            {
                if (AppStatus.CurrentStatus != ConnectionStatus.PreGame)
                {
                    return;
                }
                ChampSelectController.Instance.ApplyNewState(e.Data.ToObject<Session>());
            }
        }

        public static async void GetLocalGameVersion()
        {
            $"Determining local game version".Info();
            string? gameVersion = null;

            while (gameVersion is null || gameVersion == "")
            {
                try
                {
                    gameVersion = await API!.RequestHandler.GetResponseAsync<string>(HttpMethod.Get, "/lol-patch/v1/game-version");
                }
                catch
                {
                    // Ignored
                }
                await Task.Delay(200);
            }
            string[] patchComponents = gameVersion.Split(".");
            Versions.Client = new(int.Parse(patchComponents[0]),
                                   int.Parse(patchComponents[1]),
                                   1);
        }

        public static Dictionary<Cell, Task<string>> GetPlayersInTeam(List<Cell> team)
        {
            Dictionary<Cell, Task<string>> toFinish = new();
            team.ForEach(cell => {
                if (cell.SummonerId == 0)
                {
                    return;
                }

                try
                {
                    toFinish.Add(cell, API!.RequestHandler.GetJsonResponseAsync(HttpMethod.Get, $"lol-summoner/v1/summoners/{cell.SummonerId}"));
                }
                catch (Exception)
                {
                    "Could not fetch players for team. Is this not a custom game?".Info();
                }

            });

            return toFinish;
        }

        public static async Task<SessionTimer?> GetChampSelectSessionTimer()
        {
            try
            {
                return JsonSerializer.Deserialize<SessionTimer>(await API!.RequestHandler.GetJsonResponseAsync(HttpMethod.Get, $"/lol-champ-select/v1/session/timer"));
            }
            catch
            {
                return null;
            }
        }

        public static async void StartChampSelect()
        {
            ChampSelectStart?.Invoke(null, EventArgs.Empty);
            Session s = await API!.RequestHandler.GetResponseAsync<Session>(HttpMethod.Get, $"/lol-champ-select/v1/session");
            ChampSelectController.Instance.ApplyNewState(s);
        }

        public static void StopChampSelect()
        {
            ChampSelectStop?.Invoke(null, EventArgs.Empty);
        }
    }
}
