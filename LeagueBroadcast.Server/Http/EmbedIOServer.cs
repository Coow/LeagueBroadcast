using EmbedIO;
using EmbedIO.Files;
using Utils.Log;
using System;
using System.IO;

namespace Server.Http
{
    static class EmbedIOServer
    {
        private static WebServer? webServer;

        private static WSServer? _socketServer;

        public static WSServer SocketServer { get {
                if (_socketServer is null)
                {
                    _socketServer = new WSServer("/api");
                }

                return _socketServer;
            } }

        public static void Start(string location, int port)
        {
            var uri = $"http://{location}:{port}/";

            webServer = CreateWebServer(uri);


            webServer.RunAsync();
            $"WebServer running on {uri}".Info("EmbedIO");
        }

        public static void Restart()
        {
            throw new NotImplementedException();
        }

        public static void Stop()
        {
            webServer?.Dispose();
            $"WebServer stopped".Info("EmbedIO");
        }

        private static WebServer CreateWebServer(string url)
        {
            var webRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Cache");
            $"Server file system starting".Info();
            var server = new WebServer(o => o
                    .WithUrlPrefix(url)
                    .WithMode(HttpListenerMode.EmbedIO))
                // Add modules
                .WithLocalSessionManager()
                .WithCors()
                .WithModule(SocketServer)
                .WithModule(new FileModule("/cache",
                    new FileSystemProvider(webRoot, false))
                {
                    DirectoryLister = DirectoryLister.Html
                })
                // Static files last to avoid conflicts
                .WithStaticFolder("/frontend", $"{Directory.GetCurrentDirectory()}\\Frontend\\ingame", true, m => m
                    .WithContentCaching(true))
                .WithStaticFolder("/", $"{Directory.GetCurrentDirectory()}\\Frontend\\pickban", true, m => m
                    .WithContentCaching(true))
                ;

            // Listen for state changes.
            server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info("EmbedIO");

            return server;
        }
    }
}
