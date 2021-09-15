using Common;
using Common.Config;
using Common.Data.LeagueOfLegends;
using Common.Http;
using Utils;
using Utils.Log;
using System.Text.Json;
using Server.Config;
using Common.Config.Files;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Server.Data.Provider
{
    public static class DataDragon
    {
        private static readonly string _currentDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");

        private static int _toDownload, _downloaded;
        private static TaskCompletionSource<bool>? _downloadComplete;
        private static DataDragonConfig _cfg;

        private static int IncrementToDownload() => Interlocked.Increment(ref _toDownload);
        private static int IncrementToDownload(int count) => Interlocked.Add(ref _toDownload, count);
        private static int IncrementDownloaded() => Interlocked.Increment(ref _downloaded);
        private static int IncrementDownloaded(int count) => Interlocked.Add(ref _downloaded, count);

        public static EventHandler? LoadComplete;
        public static EventHandler<FileLoadProgressEventArgs>? FileDownloadComplete;

        public static void Startup()
        {
            "[CDrag] CommunityDragon Provider Init".Info();
            "CommunityDragon Init".UpdateLoadStatus();

            _cfg = ConfigController.Get<ComponentConfig>().DataDragon;

            if (_cfg.Patch == "latest")
            {
                "[CDrag] Getting latest versions".Info();

                new Task(async () =>
                {
                    await GetLatestGameVersion();
                    await Init();
                }).Start();

            }
            else
            {
                $"[CDrag] Using version from configuration: {_cfg.Patch}".Info();

                new Task(async () =>
                {
                    if (!StringVersion.TryParse(_cfg.Patch, out StringVersion? requestedPatch))
                    {
                        "[CDrag] Could not read requested game version. Falling back to latest".Warn();
                        await GetLatestGameVersion();
                    }
                    await Init();
                }).Start();
            }
        }

        private static async Task GetLatestGameVersion()
        {
            "Retrieving latest patch info".UpdateLoadStatus();
            "[CDrag] Retrieving latest patch info".Info();

            string? rawCDragVersionResponse = JsonDocument.Parse(await RestRequester.GetRaw($"{_cfg.CDragonRaw}/latest/content-metadata.json")).RootElement.GetProperty("version").GetString();
            if (rawCDragVersionResponse is null)
            {
                $"[CDrag] Could not get latest CDragon version. Falling back to latest DDrag version".Warn();
                using JsonDocument response = JsonDocument.Parse(await RestRequester.GetRaw($"https://ddragon.leagueoflegends.com/realms/{_cfg.Region}.json"), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                rawCDragVersionResponse = response.RootElement.GetProperty("n").GetProperty("champion").ToString();
            }
            Versions.CDrag = StringVersion.Parse(rawCDragVersionResponse.Split("+")[0]);

            $"[CDrag] Using live patch {Versions.CDrag} on platform {_cfg.Region}".Info();

            $"{Versions.CDrag.ToString(2)}, {GetLatestLocalPatch().ToString(2)}".Info();

            if (StringVersion.Parse(Versions.CDrag.ToString(2)) > StringVersion.Parse(GetLatestLocalPatch().ToString(2)))
            {
                $"[CDrag] New patch {Versions.CDrag} detected".Info();
                ConfigController.Get<RCVolusPickBanConfig>().Frontend.Patch = Versions.CDrag.ToString(2);
                await ConfigController.WriteConfigAsync<RCVolusPickBanConfig>();
            }
        }

        private static StringVersion GetLatestLocalPatch()
        {
            string patchDir = Path.Combine(_currentDir, "Cache");
            if (Directory.Exists(patchDir))
            {
                $"Found cache folder".Debug();
                return Directory.GetDirectories(patchDir).Select(Path.GetFileName).Where(dir => dir.Count(c => c == '.') == 2).Select(dir => StringVersion.Parse(dir)).Max() ?? StringVersion.Zero;
            }
                
            return new(0, 0, 0);
        }
        private static async Task Init()
        {

            Champion.All = (await RestRequester.GetAsync<HashSet<Champion>>($"{_cfg.CDragonRaw}/{_cfg.Patch}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/v1/champion-summary.json")).Where(c => c.ID > 0).ToHashSet();
            $"[CDrag] Loaded {Champion.All.Count} champions".Info();

            SummonerSpell.All = await RestRequester.GetAsync<HashSet<SummonerSpell>>($"{_cfg.CDragonRaw}/{_cfg.Patch}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/v1/summoner-spells.json");
            $"[CDrag] Loaded {SummonerSpell.All.Count} summoner spells".Info();

            Item.All = await RestRequester.GetAsync<HashSet<Item>>($"{_cfg.CDragonRaw}/{_cfg.Patch}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/v1/items.json");
            $"[CDrag] Loaded {Item.All.Count} full items".Info();

            bool result = await VerifyLocalCache(Versions.CDrag);

            LoadComplete?.Invoke(null, EventArgs.Empty);
        }

        private static async Task<bool> VerifyLocalCache(StringVersion currentPatch)
        {
            "Verifying local cache".UpdateLoadStatus();

            _downloadComplete = new TaskCompletionSource<bool>();

            currentPatch = StringVersion.Parse($"{currentPatch.ToString(2)}.1");
            string cache = _currentDir + "/Cache";
            string patchFolder = cache + $"/{currentPatch}";
            string champ = patchFolder + "/champion";
            string item = patchFolder + "/item";
            string spell = patchFolder + "/spell";

            _ = Directory.CreateDirectory(cache);
            _ = Directory.CreateDirectory(patchFolder);
            _ = Directory.CreateDirectory(champ);
            _ = Directory.CreateDirectory(item);
            _ = Directory.CreateDirectory(spell);

            "Yeeting old caches onto Dominion".UpdateLoadStatus();
            Directory.EnumerateDirectories(cache).Where(d => StringVersion.TryParse(d.Split("/")[^1].Split("\\")[^1], out StringVersion? dirVersion) && dirVersion < currentPatch).ToList().ForEach(dir =>
            {
                new DirectoryInfo(dir).Empty();
                Directory.Delete(dir);
                $"Removed Patch Cache {dir}".Info();
            });

            ConcurrentBag<string[]> failedDownloads = new();
            int toCache = IncrementToDownload(Champion.All.Count * 4 + Item.All.Count + SummonerSpell.All.Count);

            Stopwatch s = new();
            s.Start();

            await DownloadMissingChampionCache(champ, failedDownloads);
            DownloadMissingItemCache(item, failedDownloads);
            DownloadMissingSummonerSpellCache(spell, failedDownloads);

            s.Stop();
            $"[CDrag] Verified local cache in {s.ElapsedMilliseconds}ms".Debug();

            if (_toDownload == toCache)
            {
                "Local cache up to date".Info();
                return true;
            }

            $"[CDrag] Downloaded {_toDownload} assets from CommunityDragon".Info();

            if (_downloaded == _toDownload)
            {
                _downloadComplete.TrySetResult(failedDownloads.Count == 0);
            }
                
            bool updateResult = await _downloadComplete.Task;
            $"[CDrag] Downloaded missing assets".Info();

            return updateResult;
        }

        private static async Task DownloadMissingChampionCache(string location, ConcurrentBag<string[]> failedDownloads)
        {
            "[CDrag] Verifying Champion Assets".Info();
            
            foreach (Champion champ in Champion.All)
            {
                //Check if all files for the champ exist
                bool loadingExists = File.Exists($"{location}/{champ.Alias}_loading.png");
                bool splashExists = File.Exists($"{location}/{champ.Alias}_splash.png");
                bool centeredSplashExists = File.Exists($"{location}/{champ.Alias}_centered_splash.png");
                bool squareExists = File.Exists($"{location}/{champ.Alias}_square.png");

                if (loadingExists && splashExists && centeredSplashExists && squareExists)
                {
                    FileDownloadComplete?.Invoke(null, new FileLoadProgressEventArgs(champ.Alias, "Verified", IncrementDownloaded(4), _toDownload));
                    return;
                }

                //Get champ data if not all files exist
                await ExtendChampion(champ, _cfg.Patch);

                //Get missing files
                if (!loadingExists)
                {
                    DownloadAsset(champ.LoadingImg!, $"{location}/{champ.Alias}_loading.png", $"{champ.Alias}_loading.png", failedDownloads);
                }

                if (!splashExists)
                {
                    DownloadAsset(champ.SplashImg!, $"{location}/{champ.Alias}_splash.png", $"{champ.Alias}_splash.png", failedDownloads);
                }

                if (!centeredSplashExists)
                {
                    DownloadAsset(champ.SplashCenteredImg!, $"{location}/{champ.Alias}_centered_splash.png", $"{champ.Alias}_centered_splash.png", failedDownloads);
                }

                if (!squareExists)
                {
                    DownloadAsset(champ.SquareImg!, $"{location}/{champ.Alias}_square.png", $"{champ.Alias}_square.png", failedDownloads);
                }

                FileDownloadComplete?.Invoke(null, new FileLoadProgressEventArgs(champ.Alias, "Verified", IncrementDownloaded(4), _toDownload));
            };
        }


        private static void DownloadMissingItemCache(string location, ConcurrentBag<string[]> failedDownloads)
        {
            "[CDrag] Verifying Item Assets".Info();
            foreach (Item item in Item.All)
            {
                if (!File.Exists($"{location}/{item.ID}.png"))
                {
                    DownloadAsset($"{_cfg.CDragonRaw}/{_cfg.Patch}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/assets/items/icons2d/{item.IconPath.Split("/")[^1].ToLower()}", $"{location}/{item.ID}.png", $"{item.ID}.png", failedDownloads);
                }
                FileDownloadComplete?.Invoke(null, new FileLoadProgressEventArgs(item.Name, "Verified", IncrementDownloaded(), _toDownload));
            };
        }

        private static void DownloadMissingSummonerSpellCache(string location, ConcurrentBag<string[]> failedDownloads)
        {
            "[CDrag] Verifying Summoner Spell Assets".Info();
            foreach (SummonerSpell spell in SummonerSpell.All)
            {
                if (!File.Exists($"{location}/{spell.ID}.png"))
                {
                    DownloadAsset($"{_cfg.CDragonRaw}/{_cfg.Patch}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/data/spells/icons2d/{spell.IconPath.Split("/")[^1].ToLower()}", $"{location}/{spell.ID}.png", $"{spell.ID}.png", failedDownloads);
                }
                FileDownloadComplete?.Invoke(null, new FileLoadProgressEventArgs(spell.Name, "Verified", IncrementDownloaded(), _toDownload));
            };
        }

        private static void DownloadAsset(string remote, string local, string fileName, ConcurrentBag<string[]> failedDownloads)
        {
            _ = IncrementToDownload();
            $"Downloading {fileName} from {remote}".Debug();
            Task t = Task.Run(async () =>
            {
                System.Net.HttpStatusCode res = await FileDownloader.DownloadAsync(remote, local);
                if (res == System.Net.HttpStatusCode.OK)
                {
                    $"{fileName} downloaded".Debug();
                }
                else
                {
                    failedDownloads.Add(new string[] { remote, fileName });
                    $"Download {fileName} from {remote} to {local} failed: {res}".Debug();
                }

                FileDownloadComplete?.Invoke(null, new FileLoadProgressEventArgs(fileName, "Downloaded", IncrementDownloaded(), _toDownload));

                if (_downloaded >= _toDownload)
                {
                    _ = _downloadComplete!.TrySetResult(true);
                }
            });
        }

        #region ObjectExtension
        public static async Task ExtendChampion(Champion champion, string version)
        {
            using JsonDocument response = JsonDocument.Parse(await RestRequester.GetRaw($"{_cfg.CDragonRaw}{version}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/v1/champions/{champion.ID}.json"), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            JsonElement root = response.RootElement;
            JsonElement defaultSkin = root.GetProperty("skins").EnumerateArray().Single(skin => $"{skin.GetProperty("id").GetInt32()}" == $"{champion.ID}000");

            champion.SplashImg = $"{_cfg.CDragonRaw}{version}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/v1/champion-splashes/uncentered/{champion.ID}/{(defaultSkin.GetProperty("uncenteredSplashPath").GetString() ?? "").Split("/")[^1]}";
            champion.SplashCenteredImg = $"{_cfg.CDragonRaw}{version}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/v1/champion-splashes/{champion.ID}/{(defaultSkin.GetProperty("splashPath").GetString() ?? "").Split("/")[^1]}";
            champion.SquareImg = $"{_cfg.CDragonRaw}{version}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/v1/champion-icons/{(root.GetProperty("squarePortraitPath").GetString() ?? "").Split("/")[^1]}";
            champion.LoadingImg = $"{_cfg.CDragonRaw}{version}/plugins/rcp-be-lol-game-data/{_cfg.Region}/default/assets/characters/{champion.Alias.ToLower()}/skins/base/{(defaultSkin.GetProperty("loadScreenPath").GetString() ?? "").Split("/")[^1].ToLower()}";
        }

        public static void ExtendChampionLocal(Champion champion, StringVersion version)
        {
            string championPath = Path.Combine("cache", $"{version.ToString(2)}.1", "champion");
            champion.SplashImg = $"{championPath}/{ champion.Alias}_splash.jpg";
            champion.SplashCenteredImg = $"{championPath}/{champion.Alias}_centered_splash.png";
            champion.SquareImg = $"{championPath}/{ champion.Alias}_square.png";
            champion.LoadingImg = $"{championPath}/{ champion.Alias}_loading.png";
        }

        public static void ExtendSummonerLocal(SummonerSpell summoner, StringVersion version)
        {
            summoner.IconPath = Path.Combine("cache", $"{version.ToString(2)}.1", "spell", $"{summoner.ID}.png");
        }

        public static void ExtendItemLocal(Item item, StringVersion version)
        {
            item.IconPath = Path.Combine("cache", $"{version.ToString(2)}.1", "item", item.ID + ".png");
        }

        #endregion
    }

    public class FileLoadProgressEventArgs
    {
        public string FileName { get; set; } = "";

        public string Task { get; set; } = "";
        public int Completed { get; set; }
        public int Total { get; set; }


        public FileLoadProgressEventArgs(string fileName, string task, int completed, int total)
        {
            FileName = fileName;
            Task = task;
            Completed = completed;
            Total = total;
        }
    }
}
