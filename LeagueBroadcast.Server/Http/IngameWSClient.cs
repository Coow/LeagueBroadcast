using EmbedIO.WebSockets;
using Server.Events;
using Utils;
using System;
using System.Collections.Generic;

namespace Server.Http
{
    class IngameWSClient
    {
        private readonly IWebSocketContext ctx;
        private FrontEndType type;


        public IngameWSClient(IWebSocketContext ctx, List<string> types)
        {
            this.ctx = ctx;
            types.ForEach(t => {
                if(Enum.TryParse(typeof(FrontEndType), t, true, out var res)) {
                    FlagUtils.Set(ref type, (FrontEndType) res!);
                }
            });
        }
        
        public void UpdateFrontEnd(OverlayConfig config)
        {
            if(this.type.HasFlag(config.type))
            {
                EmbedIOServer.SocketServer?.SendEventAsync(ctx, config);
            }
        }

        public bool Equals(IWebSocketContext ctx)
        {
            return this.ctx.Equals(ctx);
        }
    }


    [Flags]
    public enum FrontEndType
    {
        ChampSelect,
        Ingame,
        PostGame
    }
}
