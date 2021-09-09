﻿using Utils;
using System;

namespace Common.Config
{
    public interface IFileConfig
    {
        event EventHandler FileChanged;
        event EventHandler FileRemoved;
        string Name { get; }

        string FilePath { get; }
        StringVersion FileVersion { get; set; }
        StringVersion CurrentVersion { get; }
        void RevertToDefault();
        void CheckForUpdate();
    }
}
