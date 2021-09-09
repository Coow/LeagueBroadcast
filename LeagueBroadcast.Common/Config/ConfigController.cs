using Common.Exceptions;
using Utils;
using Utils.Log;
using System.Text.Json;

namespace Common.Config
{
    public static class ConfigController
    {

        public static string ConfigDirectory { get; } = GetConfigDirectory();

        private static readonly Dictionary<Type, object> configs = InitConfigList();
        private static readonly JsonSerializerOptions SerializationOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            ReadCommentHandling = JsonCommentHandling.Skip,
            IncludeFields = true
        };

        private static readonly FileSystemWatcher watcher = InitFileSystemWatcher();
        private static readonly HashSet<string> configsToWatch = new();
        private static readonly HashSet<string> waitingForRead = new();

        #region Async

        public static async Task<T> GetAsync<T>() where T : IFileConfig
        {
            var success = configs.TryGetValue(typeof(T), out var config);
            if (!success)
            {
                config = await LoadAndGetAsync<T>();
            }
            return (T)config!;
        }

        public static async Task<bool> RegisterConfigAsync<T>(IEnumerable<string>? configDirContents = null) where T : IFileConfig
        {
            return await RegisterConfigAsync(typeof(T), configDirContents);
        }
        public static async Task<bool> RegisterConfigAsync(Type fileConfigType, IEnumerable<string>? configDirContents = null)
        {
            if (fileConfigType.ImplementsInterface(typeof(IFileConfig)))
            {
                object? fileConfigObj = Activator.CreateInstance(fileConfigType) ?? throw new InvalidConfigException("Could not instantiate config file type");
                IFileConfig castFileConfigObj = (IFileConfig)fileConfigObj;
                string fileName = (castFileConfigObj.Name.Length != 0) ? castFileConfigObj.Name : throw new InvalidConfigException("Could not determine config name when attempting to load config");

                if (castFileConfigObj.IsLoaded())
                {
                    $"Attempted to load {fileName} even though it has already been loaded".Debug();
                    return false;
                }

                $"Attempting to load {fileName}".Debug();
                configDirContents ??= Directory.GetFiles(ConfigDirectory).Select(file => Path.GetFileName(file));

                if (configDirContents.Contains(fileName))
                {
                    //TODO Update this for non json files!
                    JsonElement readConfig = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(Path.Combine(ConfigDirectory, fileName)));
                    fileConfigObj = readConfig.ToObject(fileConfigType, SerializationOptions);
                    //end json specific code

                    //Make sure config was read
                    if (fileConfigObj is null)
                    {
                        $"Could not read {fileName}".Error();
                        return false;
                    }
                    castFileConfigObj = (IFileConfig)fileConfigObj;

                    //Update config to latest file format
                    if (castFileConfigObj.FileVersion < castFileConfigObj.CurrentVersion)
                    {
                        $"{fileName} update detected".Debug();
                        castFileConfigObj.CheckForUpdate();
                        castFileConfigObj.FileVersion = castFileConfigObj.CurrentVersion;
                    }

                    CheckForNullProperties(fileConfigObj, fileName);
                    configs.Add(fileConfigType, fileConfigObj);
                    $"{JsonSerializer.Serialize(readConfig, SerializationOptions)}".Debug();
                    $"{fileName} loaded".Info();
                    return true;
                }

                $"{fileName} not found. Generating default config".Info();
                castFileConfigObj.RevertToDefault();
                castFileConfigObj.FileVersion = castFileConfigObj.CurrentVersion;
                $"{JsonSerializer.Serialize(fileConfigObj, SerializationOptions)}".Debug();
                configs.Add(fileConfigType, fileConfigObj);
                await WriteConfigAsync(fileConfigObj);
                $"{fileName} generated".Info();
                return true;
            }
            else
                throw new InvalidConfigException("Tried converting invalid object to FileConfig. Config must implement IFileConfig");
        }

        public static async Task<bool> ReloadConfigAsync(Type fileConfigType, IEnumerable<string>? configDirContents = null)
        {
            bool success = configs.TryGetValue(fileConfigType, out var cfgObject);
            if (!success || cfgObject == null)
            {
                $"Attempted reload of {fileConfigType} while not loaded. Loading now".Debug();
                success = await RegisterConfigAsync(fileConfigType);
                if (success)
                    return true;
                return false;
            }

            IFileConfig castConfig = (IFileConfig)cfgObject;

            //TODO Update this for non json files!
            JsonElement readConfig = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(Path.Combine(ConfigDirectory, castConfig.Name)));

            cfgObject = readConfig.ToObject(fileConfigType, SerializationOptions);
            //end json specific code

            //Make sure config was read
            if (cfgObject is null)
            {
                return false;
            }
            castConfig = (IFileConfig)cfgObject;
            cfgObject = readConfig.ToObject(fileConfigType, SerializationOptions);
            //end json specific code

            //Make sure config was read
            if (cfgObject is null)
            {
                $"Could not read {castConfig.Name}".Error();
                return false;
            }

            CheckForNullProperties(cfgObject, castConfig.Name);

            configs.Add(fileConfigType, cfgObject);
            $"{JsonSerializer.Serialize(readConfig, SerializationOptions)}".Debug();
            $"{castConfig.Name} reloaded".Debug();
            return true;
        }

        public static async Task WriteConfigAsync(Type fileConfigType)
        {
            if (!configs.ContainsKey(fileConfigType))
            {
                $"Attempted to write an unloaded config!".Warn();
                return;
            }

            await WriteConfigAsync(configs[fileConfigType]);
        }

        public static async Task WriteConfigAsync<T>() where T : IFileConfig
        {
            await WriteConfigAsync(typeof(T));
        }

        public static async Task WriteConfigAsync(object fileConfigObject)
        {
            IFileConfig castConfig = (IFileConfig)fileConfigObject;
            $"Writing {castConfig.Name} to file".Debug();
            //TODO Update this for non json files!
            await File.WriteAllTextAsync(Path.Combine(ConfigDirectory, castConfig.Name), JsonSerializer.Serialize(fileConfigObject, SerializationOptions));
            $"{castConfig.Name} saved".Debug();
        }

        #endregion

        #region Sync

        public static T Get<T>() where T : IFileConfig
        {
            var success = configs.TryGetValue(typeof(T), out var config);
            if (!success)
            {
                config = LoadAndGet<T>();
            }
            return (T)config!;
        }

        public static bool RegisterConfig<T>(IEnumerable<string>? configDirContents = null) where T : IFileConfig
        {
            return RegisterConfig(typeof(T), configDirContents);
        }

        public static bool RegisterConfig(Type fileConfigType, IEnumerable<string>? configDirContents = null)
        {
            if (fileConfigType.ImplementsInterface(typeof(IFileConfig)))
            {
                object? fileConfigObj = Activator.CreateInstance(fileConfigType) ?? throw new InvalidConfigException("Could not instantiate config file type");
                IFileConfig castFileConfigObj = (IFileConfig)fileConfigObj;
                string fileName = (castFileConfigObj.Name.Length != 0) ? castFileConfigObj.Name : throw new InvalidConfigException("Could not determine config name when attempting to load config");

                if (castFileConfigObj.IsLoaded())
                {
                    $"Attempted to load {fileName} even though it has already been laoded".Debug();
                    return false;
                }

                $"Attempting to load {fileName}".Debug();
                configDirContents ??= Directory.GetFiles(ConfigDirectory).Select(file => Path.GetFileName(file));

                if (configDirContents.Contains(fileName))
                {
                    //TODO Update this for non json files!
                    JsonElement readConfig = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(ConfigDirectory, fileName)));
                    fileConfigObj = readConfig.ToObject(fileConfigType, SerializationOptions);
                    //end json specific code

                    //Make sure config was read
                    if (fileConfigObj is null)
                    {
                        $"Could not read {fileName}".Error();
                        return false;
                    }
                    castFileConfigObj = (IFileConfig)fileConfigObj;

                    //Update config to latest file format
                    if (castFileConfigObj.FileVersion < castFileConfigObj.CurrentVersion)
                    {
                        $"{fileName} update detected".Debug();
                        castFileConfigObj.CheckForUpdate();
                        castFileConfigObj.FileVersion = castFileConfigObj.CurrentVersion;
                    }

                    CheckForNullProperties(fileConfigObj, fileName);
                    configs.Add(fileConfigType, fileConfigObj);
                    $"{JsonSerializer.Serialize(readConfig, SerializationOptions)}".Debug();
                    $"{fileName} loaded".Info();
                    return true;
                }

                $"{fileName} not found. Generating default config".Info();
                castFileConfigObj.RevertToDefault();
                castFileConfigObj.FileVersion = castFileConfigObj.CurrentVersion;
                $"{JsonSerializer.Serialize(fileConfigObj, SerializationOptions)}".Debug();
                configs.Add(fileConfigType, fileConfigObj);
                WriteConfig(fileConfigObj);
                $"{fileName} generated".Info();
                return true;
            }
            else
                throw new InvalidConfigException("Tried converting invalid object to FileConfig. Config must implement IFileConfig");
        }

        public static void RegisterConfigs(IEnumerable<Type> fileConfigs)
        {
            IEnumerable<string> configDirContents = Directory.GetFiles(ConfigDirectory).Select(file => Path.GetFileName(file));
            foreach (Type t in fileConfigs)
            {
                RegisterConfig(t, configDirContents);
            }
        }

        public static void RegisterConfigs(params Type[] fileConfigs)
        {
            RegisterConfigs((IEnumerable<Type>)fileConfigs);
        }

        public static bool ReloadConfig(Type fileConfigType, IEnumerable<string>? configDirContents = null)
        {
            bool success = configs.TryGetValue(fileConfigType, out var cfgObject);
            if (!success || cfgObject == null)
            {
                $"Attempted reload of {fileConfigType} while not loaded. Loading now".Debug();
                success = RegisterConfig(fileConfigType);
                if (success)
                    return true;
                return false;
            }

            IFileConfig castConfig = (IFileConfig)cfgObject;

            //TODO Update this for non json files!
            JsonElement readConfig = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(ConfigDirectory, castConfig.Name)));

            cfgObject = readConfig.ToObject(fileConfigType, SerializationOptions);
            //end json specific code

            //Make sure config was read
            if (cfgObject is null)
            {
                return false;
            }
            castConfig = (IFileConfig)cfgObject;
            cfgObject = readConfig.ToObject(fileConfigType, SerializationOptions);
            //end json specific code

            //Make sure config was read
            if (cfgObject is null)
            {
                $"Could not read {castConfig.Name}".Error();
                return false;
            }

            CheckForNullProperties(cfgObject, castConfig.Name);

            configs.Add(fileConfigType, cfgObject);
            $"{JsonSerializer.Serialize(readConfig, SerializationOptions)}".Debug();
            $"{castConfig.Name} reloaded".Debug();
            return true;
        }
        public static void WriteConfig(Type fileConfigType)
        {
            if (!configs.ContainsKey(fileConfigType))
            {
                $"Attempted to write an unloaded config!".Warn();
                return;
            }

            WriteConfig(configs[fileConfigType]);
        }

        public static void WriteConfig<T>() where T : IFileConfig
        {
            WriteConfig(typeof(T));
        }

        public static void WriteConfig(object fileConfigObject)
        {
            IFileConfig castConfig = (IFileConfig)fileConfigObject;
            $"Writing {castConfig.Name} to file".Debug();
            //TODO Update this for non json files!
            File.WriteAllText(Path.Combine(ConfigDirectory, castConfig.Name), JsonSerializer.Serialize(fileConfigObject, SerializationOptions));
            $"{castConfig.Name} saved".Debug();
        }

        #endregion

        #region ConfigWatcher
        public static bool WatchConfig(IFileConfig config)
        {
            $"Watching {config.Name} for changes".Debug();
            return configsToWatch.Add(config.Name);
        }

        public static bool UnwatchConfig(IFileConfig config)
        {
            $"Stopped watching {config.Name} for changes".Debug();
            return configsToWatch.Add(config.Name);
        }

        private static FileSystemWatcher InitFileSystemWatcher()
        {
            FileSystemWatcher watcher = new() { Filter = "*", NotifyFilter = NotifyFilters.LastWrite };
            watcher.Error += OnError;
            watcher.Changed += OnChanged;
            $"Watching Config directory for changes".Debug();
            return watcher;
        }

        private static async void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.Name is null || waitingForRead.Contains(e.Name))
                return;
            waitingForRead.Add(e.Name);

            $"{e.Name} change detected".Debug();

            Type ConfigType = configs.Values.ToHashSet().SingleOrDefault(c => ((IFileConfig)c).Name == e.Name)?.GetType() ?? throw new InvalidConfigException("Could not determine type of changed config");
            int attempts = 0;
            while (attempts < 10)
            {
                try
                {
                    await Task.Delay(500);
                    await ReloadConfigAsync(ConfigType);
                    await Task.Delay(500);
                    waitingForRead.Remove(e.Name);
                    return;
                }
                catch
                {
                    $"{watcher.Filter} locked, cannot read!".Debug();
                    attempts++;
                }
            }
            $"Timed out reading updated config {e.Name}".Warn();
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            $"File Watch error: {e.GetException()}".Warn();
        }

        #endregion

        #region LoadStatus
        public static bool IsLoaded(this IFileConfig configFile)
        {
            return configs.Any(c => c.GetType() == configFile.GetType());
        }

        public static bool IsLoaded<T>() where T : IFileConfig
        {
            return configs.Any(c => c.GetType() == typeof(T));
        }

        private static object LoadAndGet<T>() where T : IFileConfig
        {
            $"Attempting to retrieve unloaded config.".Debug();
            if (RegisterConfig(typeof(T)) && configs.TryGetValue(typeof(T), out var config))
                return config;
            throw new InvalidConfigException($"Unable to retrieve already loaded config");
        }

        private static async Task<object> LoadAndGetAsync<T>() where T : IFileConfig
        {
            $"Attempting to retrieve unloaded config".Debug();
            if (await RegisterConfigAsync(typeof(T)) && configs.TryGetValue(typeof(T), out var config))
                return config;
            throw new InvalidConfigException($"Unable to retrieve already loaded config");
        }
        #endregion

        private static void OnAppExit(object? sender, EventArgs e)
        {
            "Saving all configs to file".Info();
            try
            {
                watcher.Dispose();
                configs.ToList().ForEach(async pair =>
                {
                    await WriteConfigAsync(pair.Value);
                });
                "Configs saved".Info();
            }
            catch
            {
                "Error saving all config files. Some files may be outdated".Warn();
            }

            Logger.CloseLoggers();
        }

        private static Dictionary<Type, object> InitConfigList()
        {
            AppDomain.CurrentDomain.ProcessExit += OnAppExit;
            return new Dictionary<Type, object>();
        }

        private static string GetConfigDirectory()
        {
            string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            string configDir = Path.Combine(dataDir, "Config");
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(configDir);
            return configDir;
        }

        private static void CheckForNullProperties(object cfg, string cfgName)
        {
            //Make sure all properties of config were loaded
            if (!cfg.ArePropertiesNotNull())
            {
                $"{cfgName} not loaded correctly. Some values were not set. Things may break!".Warn();
            }
        }
    }
}
