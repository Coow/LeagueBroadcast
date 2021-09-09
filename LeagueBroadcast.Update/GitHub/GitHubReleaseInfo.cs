﻿using System.Text.Json.Serialization;

namespace Update.GitHub
{
    public sealed class GitHubReleaseInfo
    {
        [JsonPropertyName("tag_name")]
        public string Version { get; set; } = "";

        [JsonPropertyName("html_url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("assets")]
        public GitHubReleaseAsset[] Assets { get; set; } = new GitHubReleaseAsset[0];
    }
}
