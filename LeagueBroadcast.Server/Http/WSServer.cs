using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using Server.Events;
using Utils.Log;

namespace Server.Http
{
    class WSServer : WebSocketModule
    {
        private static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions() { WriteIndented = true };
        private List<IngameWSClient> clients;
        public WSServer(string urlPath) : base(urlPath, true)
        {
            this.clients = new();
        }


        protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            string message = Encoding.GetString(rxBuffer);
            dynamic res = JsonSerializer.Deserialize<dynamic>(message)!;
            string type = res.requestType;
            if(type.Equals("OverlayConfig", StringComparison.OrdinalIgnoreCase))
            {
                List<string> types = ((string)res.OverlayType).Split(",").ToList();
                if (!clients.Any(c => c.Equals(context)))
                {
                    clients.Add(new IngameWSClient(context,types));
                }

                if (types.Contains("Ingame"))
                {
                    //SendEventAsync(context, IngameOverlay.Instance);
                }
            }
            return Task.CompletedTask;
        }

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            $"New Client {context.Id} connected from {context.Origin}".Info();
            //TODO send champ select state data if PickBan is active
            //This is to enable compat with RCVolus frontends!
            /*
            if (ConfigController.Component.PickBan.IsActive)
            {
                SendEventAsync(context, new NewState(State.data));
            }
            */
            return Task.CompletedTask;
        }

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            //return SendToOthersAsync(context, "Someone left the chat room.");
            $"Client {context.Id} disconnected".Info();
            return Task.CompletedTask;
        }

        private Task SendToOthersAsync(IWebSocketContext context, string payload)
        {
            return BroadcastAsync(payload, c => c != context);
        }

        public void SendMessageToAllAsync(string payload)
        {
            BroadcastAsync(payload);
        }

        public void SendEventToAllAsync(LeagueEvent leagueEvent)
        {
            BroadcastAsync(JsonSerializer.Serialize(leagueEvent, SerializerOptions));
        }

        public void SendEventAsync(IWebSocketContext context, LeagueEvent leagueEvent)
        {
            SendAsync(context, JsonSerializer.Serialize(leagueEvent, SerializerOptions));
        }
    }
}
