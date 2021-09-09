using Common.Config;
using Utils;
using Utils.Log;
using System.Text.Json.Serialization;

namespace Client
{
    public class ClientConfig : JsonConfig
    {
        #region NonSerialized
        [JsonIgnore]
        public override string Name => "Client.json";
        [JsonIgnore]
        public override StringVersion CurrentVersion => new(1,0,0);
        #endregion

        #region Serialized
        private StringVersion _lastSkippedVersion = StringVersion.Zero;

        public StringVersion LastSkippedVersion
        {
            get { return _lastSkippedVersion; }
            set { _lastSkippedVersion = value; OnPropertyChanged(); }
        }

        #endregion

        public override void CheckForUpdate()
        {
            if (FileVersion != CurrentVersion)
            {
                $"{Name} update detected".Info();
            }
        }

        public override void RevertToDefault()
        {
            LastSkippedVersion = StringVersion.Zero;
        }
    }
}
