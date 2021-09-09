using Common.Config;
using Common.Config.Files;
using LCUSharp;
using LCUSharp.Websocket;
using Server.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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


        public static async Task<bool> InitConnectionWithinDuration(int timeoutInMS)
        {
            Task t = InitConnection();
            return await Task.WhenAny(t, Task.Delay(timeoutInMS)) == t;
        }

        public static async Task InitConnection()
        {
            API = await ConnectToClient();
            API.Disconnected += async (e, a) =>
            {
                "Client Disconnected! Attempting to reconnect...".Info();
                ClientDisconnected?.Invoke(null, EventArgs.Empty);
                await API.ReconnectAsync();
                "Client Reconnected!".Info();
                ClientConnected?.Invoke(null, EventArgs.Empty);
            };
        }

        private static async Task<LeagueClientApi> ConnectToClient()
        {
            "Connecting to League Client".Info();
            Stopwatch stopwatch = Stopwatch.StartNew();
            LeagueClientApi api = await LeagueClientApi.ConnectAsync();
            api.EventHandler.Subscribe("/lol-gameflow/v1/gameflow-phase", ClientStateChanged);
            api.EventHandler.Subscribe("/lol-champ-select/v1/session", ChampSelectChanged);
            stopwatch.Stop();
            $"Connected to League Client in {stopwatch.ElapsedMilliseconds} ms".Info();

            ClientConnected?.Invoke(null, EventArgs.Empty);

            return api;
        }

        private static void ClientStateChanged(object? sender, LeagueEvent e)
        {
            string eventType = e.Data.ToString();
            $"League State: {eventType}".Info();

            /*
            if (!eventType.Equals("InProgress") && BroadcastController.CurrentLeagueState.HasFlag(LeagueState.InProgress))
            {
                GameStop?.Invoke(null, EventArgs.Empty);
            }
            if (!eventType.Equals("ChampSelect") && BroadcastController.CurrentLeagueState.HasFlag(LeagueState.ChampSelect))
            {
                ChampSelectStop?.Invoke(null, EventArgs.Empty);
            }
            //Backup state change
            if (eventType.Equals("ChampSelect") && !BroadcastController.CurrentLeagueState.HasFlag(LeagueState.ChampSelect))
            {
                ChampSelectStart?.Invoke(null, EventArgs.Empty);
            }
            */
        }

        private static void ChampSelectChanged(object? sender, LeagueEvent e)
        {
            if (ComponentCfg.PickBan is not null && ComponentCfg.PickBan.IsActive)
            {
                /**
                if (!BroadcastController.CurrentLeagueState.HasFlag(LeagueState.ChampSelect))
                {
                    ChampSelectStart?.Invoke(this, EventArgs.Empty);
                }
                BroadcastController.Instance.PBController.ApplyNewState(e);
                */
            }
        }
    }
}
