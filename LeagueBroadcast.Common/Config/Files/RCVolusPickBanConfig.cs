using System.Drawing;
using System.Text.Json.Serialization;
using Utils;
using Utils.Log;

#pragma warning disable CS8618 
namespace Common.Config.Files
{
    public class RCVolusPickBanConfig : JsonConfig
    {
        [JsonIgnore]
        public override string Name => "RCVolusPickBan.json";

        [JsonIgnore]
        public override StringVersion CurrentVersion => new (1,0,0);

        [JsonPropertyName("frontend")]
        public RCVolusPickBanFrontendConfig Frontend { get; set; }

        public override void CheckForUpdate()
        {
            if (FileVersion != CurrentVersion)
            {
                $"{Name} update detected".Info();
            }
        }

        public override void RevertToDefault()
        {
            Frontend = RCVolusPickBanFrontendConfig.GetDefault();
        }
    }

    public class RCVolusPickBanFrontendConfig : ObservableObject
    {
        private bool _scoreEnabled;

        [JsonPropertyName("scoreEnabled")]
        public bool ScoreEnabled
        {
            get { return _scoreEnabled; }
            set { _scoreEnabled = value; OnPropertyChanged(); }
        }

        private bool _spellsEnabled;

        [JsonPropertyName("spellsEnabled")]
        public bool SpellsEnabled
        {
            get { return _spellsEnabled; }
            set { _spellsEnabled = value; OnPropertyChanged(); }
        }

        private bool _coachesEnabled;

        [JsonPropertyName("coachesEnabled")]
        public bool CoachesEnabled
        {
            get { return _coachesEnabled; }
            set { _coachesEnabled = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("blueTeam")]
        public RCVolusPickBanTeamConfig? BlueTeam { get; set; }

        [JsonPropertyName("redTeam")]
        public RCVolusPickBanTeamConfig? RedTeam { get; set; }

        private string _patch = "";
        [JsonPropertyName("patch")]
        public string Patch
        {
            get { return _patch; }
            set { _patch = value; OnPropertyChanged(); }
        }

        public static RCVolusPickBanFrontendConfig GetDefault()
        {
            return new() { ScoreEnabled = true, SpellsEnabled = true, CoachesEnabled = true, BlueTeam = RCVolusPickBanTeamConfig.GetDefault("Blue", Color.FromArgb(80, 140, 230)), RedTeam = RCVolusPickBanTeamConfig.GetDefault("Red", Color.FromArgb(239,66,67)) };
        }
    }

    public class RCVolusPickBanTeamConfig : ObservableObject {

        private string _name = "";
        [JsonPropertyName("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        private string _nameTag = "";
        [JsonPropertyName("nameTag")]
        public string NameTag
        {
            get { return _nameTag; }
            set { _nameTag = value; OnPropertyChanged(); }
        }

        private int _score;
        [JsonPropertyName("score")]
        public int Score
        {
            get { return _score; }
            set { _score = value; OnPropertyChanged(); }
        }

        private string _coach = "";
        [JsonPropertyName("coach")]
        public string Coach
        {
            get { return _coach; }
            set { _coach = value; OnPropertyChanged(); }
        }

        private string _colorString = "";
        [JsonPropertyName("color")]
        public string ColorString
        {
            get { return _colorString; }
            set { _colorString = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public Color Color
        {
            get => ColorTranslator.FromHtml(_colorString);
            set => _colorString = ColorTranslator.ToHtml(value);
        }

        public static RCVolusPickBanTeamConfig GetDefault(string teamName, Color teamColor)
        {
            return new() { Name = teamName, NameTag = teamName.Substring(0, 3), Score = 0, Coach = $"{teamName} Coach", Color = teamColor };
        }
    }
}
