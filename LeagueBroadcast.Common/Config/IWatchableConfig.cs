namespace Common.Config
{
    interface IWatchableConfig
    {
        bool AttachFileWatcher();
        bool DetachFileWatcher();
    }
}
