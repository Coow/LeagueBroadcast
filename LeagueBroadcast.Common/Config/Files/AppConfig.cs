using Utils;
using Utils.Log;
using System.Text.Json.Serialization;

#pragma warning disable CS8618
namespace Common.Config.Files
{
    public class AppConfig : JsonConfig
    {
        #region NonSerialized
        [JsonIgnore]
        public override string Name => "App.json";

        [JsonIgnore]
        public override StringVersion CurrentVersion => new(1, 0, 0);

        #endregion

        #region Serialized

        private bool _runServer;
        public bool RunServer { get { return _runServer; } set { _runServer = value; OnPropertyChanged(); } }

        private bool _saveRunServerOption;
        public bool SaveRunServerOption { get { return _saveRunServerOption; } set { _saveRunServerOption = value; OnPropertyChanged(); } }

        private bool _createDebugLog;
        public bool CreateDebugLog { get { return _createDebugLog; } set { _createDebugLog = value; OnPropertyChanged(); } }

        private bool _checkForUpdates;
        public bool CheckForUpdates { get { return _checkForUpdates; } set { _checkForUpdates = value; OnPropertyChanged(); } }

        private string _updateRepositoryUrl;
        public string UpdateRepositoryUrl { get { return _updateRepositoryUrl; } set { _updateRepositoryUrl = value; OnPropertyChanged(); } }

        private string _updateRepositoryName;
        public string UpdateRepositoryName { get { return _updateRepositoryName; } set { _updateRepositoryName = value; OnPropertyChanged(); } }

        private bool _checkForUpdatedOffsets;
        public bool CheckForUpdatedOffsets { get { return _checkForUpdatedOffsets; } set { _checkForUpdatedOffsets = value; OnPropertyChanged(); } }

        private string _offsetRepository;
        public string OffsetRepository { get { return _offsetRepository; } set { _offsetRepository = value; OnPropertyChanged(); } }

        private string _offsetPrefix;
        public string OffsetPrefix { get { return _offsetPrefix; } set { _offsetPrefix = value; OnPropertyChanged(); } }

        #endregion

        public override void RevertToDefault()
        {
            "Reverting App.json".Info();
            RunServer = true;
            SaveRunServerOption = false;
            CreateDebugLog = true;
            CheckForUpdates = true;
            UpdateRepositoryName = @"floh22/LeagueBroadcast";
            UpdateRepositoryUrl = "https://github.com/floh22/LeagueBroadcast";
            CheckForUpdatedOffsets = true;
            OffsetRepository = "https://raw.githubusercontent.com/floh22/LeagueBroadcast/v2/Offsets/";
            OffsetPrefix = "Offsets-";
        }

        public override void CheckForUpdate()
        {
            if (FileVersion != CurrentVersion)
            {
                $"{Name} update detected".Info();
            }
        }
    }
}
