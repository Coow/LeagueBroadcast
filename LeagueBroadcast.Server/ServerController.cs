using Common.Interface;
using Server.Data;
using Utils;
using Utils.Log;

using System;
using System.Threading.Tasks;
using Server.Data.Provider;
using Common;
using Server.Http;
using Farsight;
using Common.Config;
using Server.Config;
using System.Net.Http;
using Server.PreGame.ChampSelect;

namespace Server
{
    public class ServerController : TickController
    {
        public static event EventHandler? PreInitComplete, InitComplete, LateInitComplete;

        private static readonly ServerController _instance = new();

        private ServerController()
        {
            TicksPerSecond = 2;
        }

        public static void Startup()
        {
            $"Starting LeagueBroadcast Server v{StringVersion.CallingAppVersion}".Info();
            PreInit();
        }
        private static void PreInit()
        {
            try
            {
                _ = Logger.RegisterLogger<InstanceFileLogger>();
            }
            catch (InvalidOperationException)
            {
                "Client already registered required loggers".Debug();
            }

            DataDragon.LoadComplete += (s, e) => Init();
            InitComplete += (s, e) => LateInit();
            DataDragon.Startup();
        }

        public static async void Init()
        {
            ClientDataProvider.GameStart += (s, e) => AppStatus.UpdateStatus(ConnectionStatus.Ingame);
            if (await ClientDataProvider.InitConnectionWithinDuration(1000))
            {
                //Client running and connected
                InitClientConnection();
            }
            else
            {
                //Client not running. Load things later when the client connects
                ClientDataProvider.ClientConnected += ClientDataProvider_ClientConnected;
            }
            InitComplete?.Invoke(null, EventArgs.Empty);
        }

        public static void LateInit()
        {
            EmbedIOServer.Start("*", 9001);
            "Starting Webserver".UpdateLoadStatus();

            ChampSelectController.Init();

            LateInitComplete?.Invoke(null, EventArgs.Empty);
        }

        private static void ClientDataProvider_ClientConnected(object? sender, EventArgs e)
        {
            //Run once per session
            ClientDataProvider.ClientConnected -= ClientDataProvider_ClientConnected;
            InitClientConnection();
        }

        private static async void InitClientConnection()
        {
            AppStatus.UpdateStatus(ConnectionStatus.Connecting);

            ClientDataProvider.GetLocalGameVersion();

            $"Determined Client version {Versions.Client}. Updating farsight values".Info();
            FarsightConfig cfg = await ConfigController.GetAsync<FarsightConfig>();

            FarsightDataProvider.Init();
            FarsightDataProvider.ObjectOffsets = cfg.Offsets.GameObject!;
            FarsightDataProvider.GameOffsets = cfg.Offsets.Global!;

            AppStatus.UpdateStatus(ConnectionStatus.Connected);
        }
    }
}
