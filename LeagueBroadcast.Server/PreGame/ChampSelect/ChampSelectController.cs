using Common;
using Common.Config;
using Common.Interface;
using Server.Config;
using Server.Data.Provider;
using Server.Http;
using Server.PreGame.ChampSelect.Data.LCU;
using Server.PreGame.ChampSelect.Events;
using Server.PreGame.ChampSelect.StateInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Utils;
using Utils.Log;
using static Server.PreGame.ChampSelect.StateInfo.Converter;

namespace Server.PreGame.ChampSelect
{
    internal class ChampSelectController : ITickable
    {

        private static ChampSelectController? _instance;
        public static ChampSelectController Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new ChampSelectController();
                }

                return _instance;
            }
        }

        public List<Summoner> Summoners { get; set; } = new();
        public bool UpdatedThisTick { get; set; }

        private long LastTime { get; set; } = -1;
        private System.Timers.Timer HeartbeatTimer { get; set; }

        private readonly int MaxFailedAttempts = 5;
        private int FailedAttempts;

        private PickBanConnector Connector;

        public ChampSelectController()
        {
            $"[ChampSelect] Init Controller".Debug();
            Connector = new();

            HeartbeatTimer = new System.Timers.Timer() { Interval = 10000 };
            HeartbeatTimer.Elapsed += (s, e) => {
                SendHeartbeat();
            };

            PickBanComponentConfig cfg = ConfigController.Get<ComponentConfig>().PickBan;

            cfg.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case "IsActive":
                        if (cfg.IsActive)
                        {
                            Enable();
                        }
                        else
                        {
                            Disable();
                        }
                        break;
                    default:
                        break;
                }
            };
        }

        public static void Init()
        {
            if (ConfigController.Get<ComponentConfig>().PickBan.IsActive)
            {
                Instance.Enable();
                ServerController.LateInitComplete += async (s, e) => {
                    SessionTimer? t = await ClientDataProvider.GetChampSelectSessionTimer();
                    if (t is not null)
                    {
                        ClientDataProvider.StartChampSelect();
                    }
                };
            }
        }

        private void Enable()
        {
            $"[ChampSelect] Enabled Component".Debug();
            StartHeartbeat();
            ClientDataProvider.ChampSelectStart += OnEnterPickBan;
            ClientDataProvider.ChampSelectStop += OnExitPickBan;
        }

        private void Disable()
        {
            $"[ChampSelect] Disabled Component".Debug();
            StopHeartbeat();
            ClientDataProvider.ChampSelectStart -= OnEnterPickBan;
            ClientDataProvider.ChampSelectStop -= OnExitPickBan;
        }


        public async void DoTick()
        {
            //Update the timer since LCUSharp only fires event when champ select changes states
            //Obviously does not include timer update since that would ruin the point of events
            if (!UpdatedThisTick)
            {

                SessionTimer? raw = await ClientDataProvider.GetChampSelectSessionTimer();
                if (raw is null)
                {
                    "[ChampSelect] Tried retrieving pick ban timer while not active. Ignoring".Warn();

                    if (FailedAttempts++ == MaxFailedAttempts)
                    {
                        _ = TickController.RemoveTickable(this);
                        FailedAttempts = 0;
                    }
                    return;
                }

                State.data.Timer = ConvertTimer(raw);
                State.TriggerUpdate();
            }

            UpdatedThisTick = false;
        }

        public void ApplyNewState(Session? s)
        {
            if(s is null)
            {
                $"[ChampSelect] Could not apply new state".Error();
                return;
            }
            CurrentState newState = new(true, s);
            if (!State.data.ChampSelectActive)
            {
                "[ChampSelect] ChampSelect started!".Info();
                State.OnChampSelectStarted();
                // Also cache information about summoners since this wont change
                Task t = CacheSummoners(newState.Session);
                t.Wait();
            }

            LastTime = State.data.Timer;

            StateConversionOutput cleanedData = ConvertState(newState);

            CurrentAction currentActionBefore = State.data.GetCurrentAction();

            State.NewState(cleanedData);

            CurrentAction currentActionAfter = State.data.GetCurrentAction();

            if (!currentActionBefore.Equals(currentActionAfter))
            {
                CurrentAction action = State.data.RefreshAction(currentActionBefore);

                State.OnNewAction(action);
            }
        }

        public void OnEnterPickBan(object? sender, EventArgs e)
        {
            if (TickController.IsTicking(this))
            {
                return;
            }
            "[ChampSelect] Starting Champ Select".Info();
            _ = TickController.AddTickable(0, this);
            AppStatus.UpdateStatus(ConnectionStatus.PreGame);
        }

        public void OnExitPickBan(object? sender, EventArgs e)
        {
            if (State.data.ChampSelectActive)
            {
                bool finished = State.data.Timer == 0 && LastTime == 0;
                string finishedText = finished ? "finished" : "ended early";
                $"[ChampSelect] ChampSelect {finishedText}!".Info();
                AppStatus.UpdateStatus(finished ? ConnectionStatus.Ingame : ConnectionStatus.Connected);
                State.OnChampSelectEnded(finished);
                _ = TickController.RemoveTickable(this);
            }
        }

        public void StartHeartbeat()
        {
            HeartbeatTimer.Enabled = true;
        }

        public void StopHeartbeat()
        {
            HeartbeatTimer.Enabled = false;
        }

        private static void SendHeartbeat()
        {
            EmbedIOServer.SocketServer?.SendEventToAllAsync(new Heartbeat());
        }

        public async Task CacheSummoners(Session session)
        {
            //Clear to reset summoners before caching
            Summoners.Clear();

            List<Cell> blueTeam = session.MyTeam;
            List<Cell> redTeam = session.TheirTeam;

            Dictionary<Cell, Task<string>> jobs = ClientDataProvider.GetPlayersInTeam(blueTeam);
            jobs = jobs.Concat(ClientDataProvider.GetPlayersInTeam(redTeam)).ToDictionary(x => x.Key, x => x.Value);
            List<Task<string>> completedJobs = jobs.Values.ToList();
            while (completedJobs.Any())
            {
                Task<string> finished = await Task.WhenAny(completedJobs);
                Summoners.Add(JsonSerializer.Deserialize<Summoner>(await finished)!);
                _ = completedJobs.Remove(finished);
            }

            $"[ChampSelect] Cached {Summoners.Count} summoners".Info();
        }
    }
}
