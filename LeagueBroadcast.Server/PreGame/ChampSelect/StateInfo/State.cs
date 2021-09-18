using Common;
using Common.Config.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Utils.Log;

namespace Server.PreGame.ChampSelect.StateInfo
{
    internal class State
    {
        public static StateData data = new();

        public static EventHandler<StateData>? StateUpdate;
        public static EventHandler<CurrentAction>? NewAction;
        public static EventHandler? ChampSelectStarted;
        public static EventHandler<bool>? ChampSelectEnded;

        public State()
        {
            "StateData config set".Info();
            JsonSerializer.Serialize(data).Debug();
        }

        public static void NewState(Converter.StateConversionOutput state)
        {
            if (!data.BlueTeam.Equals(state.BlueTeam))
            {
                data.BlueTeam = state.BlueTeam!;
            }
            if (!data.RedTeam.Equals(state.RedTeam))
            {
                data.RedTeam = state.RedTeam!;
            }
            if (data.Timer != state.Timer)
            {
                data.Timer = state.Timer;
            }
            if (data.State != state.State)
            {
                data.State = state.State;
            }

            TriggerUpdate();
        }

        public static void OnChampSelectStarted()
        {
            data.ChampSelectActive = true;
            ChampSelectStarted?.Invoke(null, EventArgs.Empty);
            TriggerUpdate();
        }

        public static void OnChampSelectEnded(bool finished)
        {
            data.ChampSelectActive = false;
            ChampSelectEnded?.Invoke(null, finished);
            TriggerUpdate();
        }

        public static void OnNewAction(CurrentAction action)
        {
            NewAction?.Invoke(null, action);
        }

        public static void LeagueConntected()
        {
            data.LeagueConnected = true;
            TriggerUpdate();
        }

        public static void LeagueDisconnected()
        {
            data.LeagueConnected = false;
            TriggerUpdate();
        }

        public static void TriggerUpdate()
        {
            ChampSelectController.Instance.UpdatedThisTick = true;
            StateUpdate?.Invoke(null, data);
        }

        public static RCVolusPickBanConfig Config => data.Config;

    }
}
