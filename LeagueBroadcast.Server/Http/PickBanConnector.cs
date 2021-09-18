using Common.Config;
using Common.Interface;
using Server.Config;
using Server.Events;
using Server.PreGame.ChampSelect.Events;
using Server.PreGame.ChampSelect.StateInfo;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utils.Log;

namespace Server.Http
{
    class PickBanConnector : ITickable
    {   
        private ConcurrentDictionary<LeagueEvent, double> EventQueue;
        private ClientConnectorMode connectionMode;
        public PickBanConnector()
        {
            $"[ChampSelect] Init Connector".Debug();
            EventQueue = new();

            ConfigController.Get<ComponentConfig>().PickBan.PropertyChanged += (s, e) => {
                if(e.PropertyName!.Equals("UseDelay", StringComparison.Ordinal)) {
                    ChangeConnectionType();
                }
            };

            ChangeConnectionType();
        }

        public void DoTick()
        {
            EventQueue.Keys.ToList().ForEach(key => {
                EventQueue[key] -= TickController.TickRateInMS;
            });

            List<KeyValuePair<LeagueEvent, double>> toRemove = EventQueue.Where(k => k.Value <= 0).ToList();
            toRemove.ForEach(async e => {
                EmbedIOServer.SocketServer.SendEventToAllAsync(e.Key);
                _ = EventQueue.TryRemove(e.Key, out var val);
                await Task.Delay(50);
            });
        }

        public void UseDelayPickBan()
        {
            //Ignore same mode requests
            if (connectionMode == ClientConnectorMode.DELAYED)
            {
                return;
            }

            if (State.data.ChampSelectActive)
            {
                "[ChampSelect] Tried enabling Champ Select Delay while active. Enable this while not in P&B!".Info();
                ConfigController.Get<ComponentConfig>().PickBan.UseDelay = false;
                return;
            }

            //Elegant code :) But it works, swap out direct events for queue
            State.StateUpdate -= SendDirect;
            State.StateUpdate += AddToQueue;
            State.NewAction -= SendDirect;
            State.NewAction += AddToQueue;
            State.ChampSelectStarted -= SendStartDirect;
            State.ChampSelectStarted += AddStartToQueue;
            State.ChampSelectEnded -= SendEndDirect;
            State.ChampSelectEnded += AddEndToQueue;

            _ = TickController.AddTickable(this);
            connectionMode = ClientConnectorMode.DELAYED;
            "[ChampSelect] Using delayed PickBan".Info();
        }

        public void UseDirectPickBan()
        {
            //Ignore same mode requests
            if (connectionMode == ClientConnectorMode.DIRECT)
                return;
            if (EventQueue.Count != 0)
            {
                "[ChampSelect] Disabled Champ Select Delay while P&B was still active! This might cause some errors".Warn();
                EventQueue.Keys.ToList().ForEach(e => {
                    EmbedIOServer.SocketServer.SendEventToAllAsync(e);
                });
                EventQueue.Clear();
            }

            //Swap out queue for direct events
            State.StateUpdate -= AddToQueue;
            State.StateUpdate += SendDirect;
            State.NewAction -= AddToQueue;
            State.NewAction += SendDirect;
            State.ChampSelectStarted -= AddStartToQueue;
            State.ChampSelectStarted += SendStartDirect;
            State.ChampSelectEnded -= AddEndToQueue;
            State.ChampSelectEnded += SendEndDirect;

            _ = TickController.RemoveTickable(this);
            connectionMode = ClientConnectorMode.DIRECT;
            "[ChampSelect] Using direct PickBan".Info();
        }

        private void AddToQueue(object sender, StateData e)
        {
            EventQueue.TryAdd(new NewState(new StateData(e)), ConfigController.Get<ComponentConfig>().PickBan.DelayAmount);
        }

        private void AddToQueue(object sender, CurrentAction e)
        {
            EventQueue.TryAdd(new NewActionEvent(new CurrentAction(e)), ConfigController.Get<ComponentConfig>().PickBan.DelayAmount);
        }

        private void AddStartToQueue(object sender, EventArgs e)
        {
            EventQueue.TryAdd(new ChampSelectStartEvent(), ConfigController.Get<ComponentConfig>().PickBan.DelayAmount);
        }

        private void AddEndToQueue(object sender, bool finished)
        {
            //Make sure that champ select went all the way
            //otherwise send the end event directly if champ select has already started on the frontend
            //or just not send anything at all if it never got that far
            //This eliminates champ select remakes from ever even showing up
            if (finished)
            {
                EventQueue.TryAdd(new ChampSelectEndEvent(), ConfigController.Get<ComponentConfig>().PickBan.DelayAmount);
                return;
            }

            //If the EventQueue does not contain a Champ Select Start Event and Ended has just been fired
            //That means that Start has already been sent, so we have to send an End event. Don't otherwise
            if (!EventQueue.Keys.ToList().Contains(new ChampSelectStartEvent()))
            {
                EmbedIOServer.SocketServer.SendEventToAllAsync(new NewState(State.data));
                EmbedIOServer.SocketServer.SendEventToAllAsync(new ChampSelectEndEvent());
            }

            EventQueue.Clear();
        }
        private void SendStartDirect(object sender, EventArgs e)
        {
            EmbedIOServer.SocketServer.SendEventToAllAsync(new ChampSelectStartEvent());
        }

        private void SendEndDirect(object sender, bool finished)
        {
            EmbedIOServer.SocketServer.SendEventToAllAsync(new ChampSelectEndEvent());
        }

        private void SendDirect(object sender, StateData e)
        {
            EmbedIOServer.SocketServer.SendEventToAllAsync(new NewState(e));
        }

        private void SendDirect(object sender, CurrentAction e)
        {
            EmbedIOServer.SocketServer.SendEventToAllAsync(new NewActionEvent(e));
        }

        private void ChangeConnectionType()
        {
            
            if (ConfigController.Get<ComponentConfig>().PickBan.UseDelay)
            {
                UseDelayPickBan();
                return;
            }
            UseDirectPickBan();
        }

        public enum ClientConnectorMode
        {
            NONE,
            DIRECT,
            DELAYED
        }
        
    }
}
