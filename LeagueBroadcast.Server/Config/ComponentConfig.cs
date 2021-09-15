using Common.Config;
using System.Text.Json.Serialization;
using Utils;
using Utils.Log;

#pragma warning disable CS8618
namespace Server.Config
{
    public class ComponentConfig : JsonConfig
    {
        [JsonIgnore]
        public override string Name => "Component.json";
        [JsonIgnore]
        public override StringVersion CurrentVersion => new(1, 3, 0);

        public DataDragonConfig DataDragon { get; set; }
        public PickBanComponentConfig PickBan { get; set; }
        public IngameComponentConfig Ingame { get; set; }

        private List<string> _leagueInstallLocations = new();

        public List<string> LeagueInstallLocations
        {
            get { return _leagueInstallLocations; }
            set { _leagueInstallLocations = value; OnPropertyChanged(); }
        }


        public override void CheckForUpdate()
        {
            if (FileVersion != CurrentVersion)
            {
                $"{Name} update detected".Info();
            }

            FileVersion = CurrentVersion;
        }

        public override void RevertToDefault()
        {
            LeagueInstallLocations = new List<string>() { "C:\\Riot Games\\League of Legends" };
            DataDragon = new DataDragonConfig()
            {
                MinimumItemGoldCost = 2000,
                Region = "global",
                Locale = "en_US",
                Patch = "latest",
                CDragonRaw = "https://raw.communitydragon.org/"
            };
            Ingame = new()
            {
                IsActive = true,
                UseFarsightAPI = true,
                UseLiveEventAPI = true
            };
            PickBan = new()
            {
                IsActive = true,

            };
        }
    }

    public class DataDragonConfig : ObservableObject
    {
        private string _patch = "";
        public string Patch
        {
            get { return _patch; }
            set { _patch = value; OnPropertyChanged(); }
        }

        private string _cDragonRaw = "";
        public string CDragonRaw
        {
            get { return _cDragonRaw; }
            set { _cDragonRaw = value; OnPropertyChanged(); }
        }

        private string _region = "";

        public string Region
        {
            get { return _region; }
            set { _region = value; OnPropertyChanged(); }
        }

        private string _locale = "";

        public string Locale
        {
            get { return _locale; }
            set { _locale = value; OnPropertyChanged(); }
        }

        private int _minimumItemGoldCost;

        public int MinimumItemGoldCost
        {
            get { return _minimumItemGoldCost; }
            set { _minimumItemGoldCost = value; OnPropertyChanged(); }
        }

    }

    public class IngameComponentConfig : ObservableObject
    {
        private bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; OnPropertyChanged(); }
        }

        private bool _useFarsightAPI;

        public bool UseFarsightAPI
        {
            get { return _useFarsightAPI; }
            set { _useFarsightAPI = value; OnPropertyChanged(); }
        }

        private bool _useLiveEventAPI;

        public bool UseLiveEventAPI
        {
            get { return _useLiveEventAPI; }
            set { _useLiveEventAPI = value; OnPropertyChanged(); }
        }


    }

    public class PickBanComponentConfig : ObservableObject
    {
        private bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; OnPropertyChanged(); }
        }
    }
}
