using Common;
using Common.Config;
using Common.Config.Files;
using Common.Http;
using Farsight;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Utils;
using Utils.Log;

namespace Server.Config
{
    public class FarsightConfig : JsonConfig
    {
        #region NonSerialized
        [JsonIgnore]
        public override string Name => "Farsight.json";

        [JsonIgnore]
        public override StringVersion CurrentVersion => new(2, 0, 0);
        #endregion

        #region Serialized
        public StringVersion GameVersion { get; set; } = new(0);
        public Offsets Offsets { get; set; } = new();
        #endregion

        public override void CheckForUpdate()
        {
            if (FileVersion < CurrentVersion)
            {
                //No file update currently
            }

            if (GameVersion < Versions.Client)
                RevertToDefault();
        }

        public override void RevertToDefault()
        {
            AppConfig appCfg = ConfigController.Get<AppConfig>();

            if (!appCfg.CheckForUpdatedOffsets)
            {
                $"Offset Update disabled".Info();
                return;
            }

            string remoteUri = $"{appCfg.OffsetRepository}{appCfg.OffsetPrefix}{Versions.Client}.json";

            $"Fetching new offsets from {remoteUri}".Info();
            try
            {
                string remoteContent = RestRequester.GetRaw(remoteUri).Result;
                FarsightConfig? remoteCfg = JsonSerializer.Deserialize<FarsightConfig>(remoteContent);

                if(remoteCfg is null || remoteContent.Length == 0)
                {
                    $"Updated offsets not found".Error();
                    FarsightDataProvider.ShouldRun = false;
                    return;
                }
                $"{remoteContent}".Debug();
                $"{JsonSerializer.Serialize(remoteCfg)}".Debug();
                remoteCfg.CopyProperties(this);
                $"Offsets updated to {Versions.Client}".Info();
            }
            catch (Exception e)
            {
                FarsightDataProvider.ShouldRun = false;
                $"Could not update offsets: \n {e.Message}".Warn();
            }
        }
    }

    public class Offsets
    {
        public FarsightDataProvider.Offsets? Global { get; set; }
        public GameObject.Offsets? GameObject { get; set; }
    }
}
