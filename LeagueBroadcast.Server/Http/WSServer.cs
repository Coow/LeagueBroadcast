using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common.Config;
using EmbedIO.WebSockets;
using Server.Config;
using Server.Events;
using Server.PreGame.ChampSelect.Events;
using Server.PreGame.ChampSelect.StateInfo;
using Utils.Log;

namespace Server.Http
{
    class WSServer : WebSocketModule
    {
        private static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions() { WriteIndented = true };
        private List<IngameWSClient> clients;
        private HashSet<IWebSocketContext> connections;
        public WSServer(string urlPath) : base(urlPath, true)
        {
            this.clients = new();
            this.connections = new();
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
            
            if (ConfigController.Get<ComponentConfig>().PickBan.IsActive)
            {
                SendEventAsync(context, new NewState(State.data));
            }

            connections.Add(context);
            return Task.CompletedTask;
        }

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            //return SendToOthersAsync(context, "Someone left the chat room.");
            $"Client {context.Id} disconnected".Info();
            connections.Remove(context);
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
            string toSend = JsonSerializer.Serialize((object)leagueEvent, SerializerOptions);
            /*
            foreach(IWebSocketContext c in connections)
            {
                SendAsync(c, toSend);
            }
            */
            BroadcastAsync(toSend);
        }

        public void SendEventAsync(IWebSocketContext context, LeagueEvent leagueEvent)
        {
            string toSend = JsonSerializer.Serialize((object)leagueEvent, SerializerOptions);
            _ = SendAsync(context, toSend);
        }
    }
}
