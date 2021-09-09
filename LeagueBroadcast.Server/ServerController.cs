using Common.Interface;
using Server.Data;
using Utils;
using Utils.Log;

using System;
using System.Threading.Tasks;
using Server.Data.Provider;
using Common;
using Server.Http;

namespace Server
{
    public class ServerController : TickController
    {
        public static event EventHandler? PreInitComplete, InitComplete, LateInitComplete;

        public static StringVersion LocalGameVersion { get; set; } = new(0, 0, 0);

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

        private static void ClientDataProvider_ClientConnected(object? sender, EventArgs e)
        {
            //Run once per session
            ClientDataProvider.ClientConnected -= ClientDataProvider_ClientConnected;
            InitClientConnection();
        }

        private static async void InitClientConnection()
        {
            AppStatus.UpdateStatus(ConnectionStatus.Connecting);

                string[] patchComponents = (await ClientDataProvider.API!.RequestHandler.GetResponseAsync<string>(HttpMethod.Get, "/lol-patch/v1/game-version")).Split(".");
                LocalGameVersion = new(int.Parse(patchComponents[0]),
                                       int.Parse(patchComponents[1]),
                                       1);


            AppStatus.UpdateStatus(ConnectionStatus.Connected);
        }

        public static void LateInit()
        {

            EmbedIOServer.Start("*", 9001);
            "Starting Webserver".UpdateLoadStatus();
            LateInitComplete?.Invoke(null, EventArgs.Empty);
        }
    }
}
