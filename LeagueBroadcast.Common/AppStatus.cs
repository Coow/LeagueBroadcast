using System;
using Utils;

namespace Common
{
    public static class AppStatus
    {
        public static event EventHandler<string>? LoadStatusUpdate;
        public static event EventHandler<ConnectionStatus>? StatusUpdate;
        public static event EventHandler<LoadProgressUpdateEventArgs>? LoadProgressUpdate;

        public static ConnectionStatus CurrentStatus { get; set; }

        public static void UpdateLoadStatus(this string status)
        {
            LoadStatusUpdate?.Invoke(null, status);
        }

        public static void UpdateStatus(this ConnectionStatus status)
        {
            CurrentStatus = status;
            StatusUpdate?.Invoke(null, status);
        }

    }

    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        PreGame,
        Ingame,
        PostGame
    }

    public enum LoadStatus
    {
        PreInit = 3,
        CDragon = 5,
        Init = 80,
        PostInit = 95,
        FinishInit = 100
    }

    public class LoadProgressUpdateEventArgs
    {
        public LoadStatus Status { get; set; }
        public int Progress { get; set; }

        
    }
}
